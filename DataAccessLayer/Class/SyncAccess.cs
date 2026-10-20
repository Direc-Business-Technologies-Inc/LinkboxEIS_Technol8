using DomainLayer.ViewModels;
//using Sap.Data.Hana;
using System;
using System.Data;
using System.Data.SqlClient;
using ClosedXML.Excel;
using System.IO;
using NPOI.HSSF.UserModel;
using System.Net;
using System.Text;
using System.Collections.Generic;
using System.Linq;

namespace DataAccessLayer.Class
{
    public class SyncAccess
    {

        public delegate string ReturnMessage(string error);
        public static DataTable Execute(SyncViewModel model, ReturnMessage returnMessage)
        {

            string Server = "", User = "", Password = "", Database = "", Query = "", DbVersion = "";
            foreach (var item in model.SyncQueryView)
            {

                Server = item.IpAddress;
                User = item.DbUser;
                Password = item.DbPass;
                Database = item.DbName;
                Query = item.QueryString;
                DbVersion = item.DbVersion;
            }

            string conn = "DRIVER={HDBODBC32};SERVERNODE=" + Server + ":30015;UID=" + User + ";PWD=" + Password + ";CS=" + Database;
            try
            {
                using (DataTable dt = new DataTable())
                {  //2023/07/28 - I commented this because Error in posting in PCII
                   //using (HanaConnection con = new HanaConnection(conn))
                   //{
                   //    using (HanaCommand cmd = new HanaCommand(Query, con))
                   //    {
                   //        HanaDataAdapter da = new HanaDataAdapter(cmd);
                   //        con.Open();
                   //        da.Fill(dt);
                   //        con.Close();
                    return dt;
                    //    }
                    //}
                }
            }
            catch (Exception ex)
            {
                //frmMain.NotiMsg(ex.Message, Color.Red);
                returnMessage(ex.Message);
                return null;
            }
        }

        public static DataTable Execute2(SyncViewModel model, ReturnMessage returnMessage)
        {
            string Server = "", User = "", Password = "", Database = "", Query = "", DbVersion = "";
            foreach (var item in model.SyncQueryView)
            {
                Server = item.IpAddress;
                User = item.DbUser;
                Password = item.DbPass;
                Database = item.DbName;
                Query = item.QueryString;
                DbVersion = item.DbVersion;
            }

            using (SqlConnection conn = new SqlConnection(@"Data Source=" + Server + ";Initial Catalog=" + Database + ";Persist Security Info=True;User ID="
                + User + ";Password=" + Password))
            {
                try
                {
                    string query = Query;

                    using (SqlDataAdapter da = new SqlDataAdapter(query, conn))
                    {
                        conn.Open();
                        DataTable dt = new DataTable();
                        da.Fill(dt);
                        return dt;
                    }
                }
                catch (Exception ex)
                {
                    returnMessage(ex.Message);
                    return null;
                }
            }
        }


        public static void ExportData(DataTable dt, SyncViewModel model)
        {
            string PathDirectory = "", FileType = "", QueryCode = "", user = "", pass = "";
            foreach (var item in model.SyncQueryView)
            {
                PathDirectory = item.Path == null || item.Path == "" ? item.RemotePath : item.Path;
                pass = item.RemotePassword;
                user = item.RemoteUser;
                FileType = item.FileType;
                QueryCode = item.QueryCode;
            }
            bool contains = PathDirectory.Contains("ftp");
            if (!contains)
            {

                if (!Directory.Exists($@"{PathDirectory}"))
                {
                    Directory.CreateDirectory($@"{PathDirectory}");
                }
            }

            string name = QueryCode;
            string extension = FileType;
            if (QueryCode == "IT" || QueryCode == "DC")
            {
                name = QueryCode + DateTime.Now.ToString("mmddyy");
            }
            string FilePath = $@"{PathDirectory}/{name.Replace(" ","")}.{extension}";

            if (extension.ToUpper() == "XLSX")
            {
                XLWorkbook wb = new XLWorkbook();
                wb.Worksheets.Add(dt, name);
                var stream = new MemoryStream();
                wb.SaveAs(stream);

                if (contains)
                {
                    DataAccess.SaveToFTP(stream, FilePath,user,pass);
                    stream.Close();
                }
                else
                {
                    wb.SaveAs($@"{PathDirectory}\{name}.{extension}");
                }

            }
            else if (extension.ToUpper() == "XLS")
            {
                HSSFWorkbook workbook = new HSSFWorkbook();
                var sheet = workbook.CreateSheet(QueryCode);
                var headerRow = sheet.CreateRow(0);
                for (int i = 0; i < dt.Columns.Count; i++)
                {
                    var cell = headerRow.CreateCell(i);
                    cell.SetCellValue(dt.Columns[i].ToString());
                }

                //Below loop is fill content  
                for (int i = 0; i < dt.Rows.Count; i++)
                {
                    var rowIndex = i + 1;
                    var row = sheet.CreateRow(rowIndex);

                    for (int j = 0; j < dt.Columns.Count; j++)
                    {
                        var cell = row.CreateCell(j);
                        var o = dt.Rows[i].ItemArray[j].ToString();
                        cell.SetCellValue(o.ToString());
                    }
                }

                var stream = new MemoryStream();
                workbook.Write(stream);

                if (contains)
                {
                    DataAccess.SaveToFTP(stream, FilePath, user, pass);
                    stream.Close();
                }
                else
                {
                    FileStream file = new FileStream(FilePath, FileMode.CreateNew, FileAccess.Write);
                    stream.WriteTo(file);
                    file.Close();
                    stream.Close();
                }

            }
            else
            {
                StringBuilder sb = new StringBuilder();
                IEnumerable<string> columnNames = dt.Columns.Cast<DataColumn>().
                                                  Select(column => column.ColumnName);
                sb.AppendLine(string.Join(",", columnNames));

                foreach (DataRow row in dt.Rows)
                {
                    IEnumerable<string> fields = row.ItemArray.Select(field => field.ToString());
                    sb.AppendLine(string.Join(",", fields));
                }

                if (contains)
                {
                    byte[] byteArray = Encoding.ASCII.GetBytes(sb.ToString());
                    var x = sb.ToString();
                    var stream = new MemoryStream(byteArray);
                    DataAccess.SaveToFTP(stream, FilePath, user, pass);
                    stream.Close();
                }
                else
                {
                    File.WriteAllText(FilePath, sb.ToString());
                }
            }

        }

    }
}