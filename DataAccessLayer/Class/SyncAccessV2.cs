using DomainLayer.ViewModels;
//using Sap.Data.Hana;
using System;
using System.Data;
using System.Data.SqlClient;
using ClosedXML.Excel;
using System.IO;
using System.Collections.Generic;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using NPOI.HSSF.UserModel;
using System.Net;
using System.Text;
using System.Linq;
namespace DataAccessLayer.Class
{
    public class SyncAccessV2
    {
        public static DataTable Execute(string IpAddress,string DbVersion,string DbUser,string DbPass,string DbName,string Query)
        {
            try
            {

                if (DbVersion.ToLower().Contains("hana"))
                {
                    string conn = "DRIVER={HDBODBC32};SERVERNODE=" + IpAddress + ":30015;" +
                            "UID=" + DbUser + ";PWD=" + DbPass + ";CS=" + DbName;
                    using (DataTable dt = new DataTable())
                    {
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
                else
                {
                    string con = QueryAccess.MSSQL_conString(IpAddress, DbUser,DbPass, DbName);

                    using (SqlConnection conn = new SqlConnection(con))
                    {

                        using (SqlDataAdapter da = new SqlDataAdapter(Query, conn))
                        {
                            conn.Open();
                            DataTable dt = new DataTable();
                            da.Fill(dt);
                            return dt;
                        }

                    }


                }
            }

            catch (Exception ex)
            {
                //frmMain.NotiMsg(ex.Message, Color.Red);

                return null;
            }
        }

        public static void ExportData(DataTable dt,string LocalPath,string FtpPath,string FtpUser,string FtpPass,string FileType,string Code)
        {
            string path = LocalPath == "" ? FtpPath : LocalPath;
            bool contains = LocalPath.IndexOf("ftp", StringComparison.OrdinalIgnoreCase) >= 0;
            if (!contains)
            {
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }
            }

            string fileName = Path.GetFileNameWithoutExtension(FileType);
            string name = Code;
            string extension = FileType;
            if (Code == "IT" || Code == "DC")
            {
                name = Code + DateTime.Now.ToString("mmddyy");
            }
            string FilePath = $@"{LocalPath}\{name}.{extension}";

            if (extension.ToUpper() == "XLSX")
            {
                XLWorkbook wb = new XLWorkbook();
                wb.Worksheets.Add(dt, fileName);
                var stream = new MemoryStream();
                wb.SaveAs(stream);

                if (contains)
                {
                    DataAccess.SaveToFTP(stream, FilePath, FtpUser, FtpPass);
                    stream.Close();
                }
                else
                {
                    wb.SaveAs($@"{path}\{name}.{extension}");
                }
                wb.SaveAs(FilePath);

            }
            else if (extension.ToUpper() == "XLS")
            {
                HSSFWorkbook workbook = new HSSFWorkbook();
                var sheet = workbook.CreateSheet(Code);
                var headerRow = sheet.CreateRow(0);
                for (int i = 0; i < dt.Columns.Count; i++)
                {
                    var cell = headerRow.CreateCell(i);
                    cell.SetCellValue(dt.Columns[i].ToString());
                }
                
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
                    DataAccess.SaveToFTP(stream, FilePath, FtpUser,FtpPass);
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
                    var stream = new MemoryStream(byteArray);
                    DataAccess.SaveToFTP(stream, FilePath, FtpUser, FtpPass);
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