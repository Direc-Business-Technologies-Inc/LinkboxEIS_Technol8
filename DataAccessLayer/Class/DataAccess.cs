
using ClosedXML.Excel;
using DocumentFormat.OpenXml.Drawing;
using DocumentFormat.OpenXml.Spreadsheet;
using DomainLayer.Models;
using DomainLayer.ViewModels;
//using Sap.Data.Hana;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using xcel = ClosedXML;
using Excel = Microsoft.Office.Interop.Excel;
using System.Runtime.Remoting.Messaging;
using System.Configuration;
using NPOI.SS.Formula.Functions;
using MySqlConnector;
using System.Drawing.Imaging;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Threading.Tasks;
using DocumentFormat.OpenXml.Wordprocessing;
using NPOI.OpenXmlFormats.Spreadsheet;
using Microsoft.VisualBasic.FileIO;
using Newtonsoft.Json.Linq;
using CsvHelper;
using System.Text;
using System.Transactions;
namespace DataAccessLayer.Class
{
    public class DataAccess
    {

        public static List<string> ConnectionTypes()
        {
            List<string> types = new List<string>();
            types.Add("MsSQL");
            types.Add("MySQL");
            types.Add("HANA");
            return types;
        }

        #region MYSQL_CONNECTION
        public static string GetMySqlConn(string host, string port, string db, string user, string pwd)
        {
            string str = "SERVER=" + host + ";PORT=" + port + ";DATABASE=" + db + ";UID=" + user + ";PASSWORD=" + pwd + "; Allow User Variables=True";
            return str;
        }
        public static DataTable MySqlSelect(string query, string constr, out string errmsg)
        {
            DataTable dt = new DataTable();
            try
            {
                using (MySqlConnection conn = new MySqlConnection(constr))
                {
                    using (MySqlCommand cmd = new MySqlCommand(query, conn))
                    {
                        cmd.CommandTimeout = 0;
                        MySqlDataAdapter da = new MySqlDataAdapter(cmd);
                        conn.Open();
                        da.Fill(dt);
                        conn.Close();
                        errmsg = "";
                        return dt;

                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("error - " + ex.Message);
                //errmsg = ex.Message;
                //return dt;
            }
        }
        #endregion



        //Saving of added udf fields in created table
        //public static void AddHeaderUdf(int Check, List<string[]> TableFields, string TableName, int UserId, int MapId, int UpdateId)
        //{
        //    foreach (var item in TableFields)
        //    {
        //        Header header = new Header();
        //        header.MapId = Check == 1 ? MapId : Convert.ToInt32(UpdateId);
        //        header.TableName = TableName;
        //        header.SAPHeaderField = item[0].ToString();
        //        header.AddonHeaderField = item[1].ToString();
        //        header.DataType = item[2].ToString();
        //        header.Length = item[3].ToString();
        //        header.IsRequired = false;
        //        header.CreateDate = DateTime.Now;
        //        header.CreateUserID = UserId;
        //        //Header.Add(header);
        //    }
        //}

        //public static void AddRowUdf(int Check, List<string[]> TableFields, string TableName, int UserId, int MapId, int UpdateId)
        //{
        //    foreach (var item in TableFields)
        //    {
        //        Row row = new Row();
        //        row.MapId = Check == 1 ? MapId : Convert.ToInt32(UpdateId);
        //        row.TableName = TableName;
        //        row.SAPRowField = item[0].ToString();
        //        row.AddonRowField = item[1].ToString();
        //        row.DataType = item[2].ToString();
        //        row.Length = item[3].ToString();
        //        row.IsRequired = false;
        //        row.CreateDate = DateTime.Now;
        //        row.CreateUserID = UserId;
        //        //Row.Add(row);
        //    }
        //}

        public static DataTable Select(string ConString, string query)
        {
            try
            {
                using (DataTable dt = new DataTable())
                {
                    using (SqlConnection con = new SqlConnection(ConString))
                    {
                        using (SqlCommand cmd = new SqlCommand(query, con))
                        {
                            SqlDataAdapter da = new SqlDataAdapter(cmd);
                            con.Open();
                            da.Fill(dt);
                            con.Close();
                            return dt;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                //frmMain.NotiMsg(ex.Message, Color.Red);
                string err = ex.Message;
                DataTable dt = new DataTable();
                return dt;
            }
        }



        public static DataTable Select(string type, string Server, string User, string Password, string Database, string query)
        {
            try
            {
                using (DataTable dt = new DataTable())
                {
                    if (type.Contains("HANA"))
                    {
                        //using (HanaConnection con = new HanaConnection(QueryAccess.HANA_conString(Server, User, Password, Database)))
                        //{
                        //    using (HanaCommand cmd = new HanaCommand(query, con))
                        //    {
                        //        HanaDataAdapter da = new HanaDataAdapter(cmd);
                        //        con.Open();
                        //        da.Fill(dt);
                        //        con.Close();
                        return dt;
                        //    }
                        //}
                    }
                    else
                    {
                        using (SqlConnection con = new SqlConnection(QueryAccess.MSSQL_conString(Server, User, Password, Database)))
                        {
                            using (SqlCommand cmd = new SqlCommand(query, con))
                            {
                                SqlDataAdapter da = new SqlDataAdapter(cmd);
                                con.Open();
                                da.Fill(dt);
                                con.Close();
                                return dt;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                //frmMain.NotiMsg(ex.Message, Color.Red);
                string err = ex.Message;
                return null;
            }
        }

        public static DataTable SelectHana(string ConString, string query)
        {
            try
            {  //2023/07/28 - I commented this because Error in posting in PCII
                using (DataTable dt = new DataTable())
                {
                    //    using (HanaConnection con = new HanaConnection(ConString))
                    //    {
                    //        using (HanaCommand cmd = new HanaCommand(query, con))
                    //        {
                    //            HanaDataAdapter da = new HanaDataAdapter(cmd);
                    //            con.Open();
                    //            da.Fill(dt);
                    //            con.Close();
                    return dt;
                    //        }
                    //    }
                }
            }
            catch (Exception ex)
            {
                //frmMain.NotiMsg(ex.Message, Color.Red);
                return null;
            }
        }

        public static Boolean Execute(string ConString, string query)
        {
            Boolean _bool = false;
            try
            {

                using (SqlConnection con = new SqlConnection(ConString))
                {
                    SqlCommand cmd = new SqlCommand();
                    cmd = con.CreateCommand();
                    con.Open();
                    cmd.CommandText = query;
                    cmd.ExecuteNonQuery();
                    _bool = true;
                }
            }
            catch (Exception ex)
            {
                //frmMain.NotiMsg(ex.Message, Color.Red);
            }
            return _bool;
        }

        public static Boolean ExecuteHana(string ConString, string query)
        {
            Boolean _bool = false;
            try
            {

                //using (HanaConnection con = new HanaConnection(ConString))
                //{
                //    HanaCommand cmd = new HanaCommand();
                //    cmd = con.CreateCommand();
                //    con.Open();
                //    cmd.CommandText = query;
                //    cmd.ExecuteNonQuery();
                //    _bool = true;
                //}
            }
            catch (Exception ex)
            {
                //frmMain.NotiMsg(ex.Message, Color.Red);
            }
            return _bool;
        }

        public static string Execute(string ConString, string query, string DocEntry)
        {
            string ret = "Error";
            try
            {

                using (SqlConnection con = new SqlConnection(ConString))
                {
                    SqlCommand cmd = new SqlCommand();
                    cmd = con.CreateCommand();
                    con.Open();
                    cmd.CommandText = query;
                    cmd.ExecuteNonQuery();
                    ret = "";
                }
            }
            catch (Exception ex)
            {
                ret = $@"Error posting. Key: {DocEntry} " + ex.Message;
                //frmMain.NotiMsg(ex.Message, Color.Red);
            }
            return ret;
        }

        public static string Execute(string type, string Server, string User, string Password, string Database, string query, string DocEntry)
        {
            string ret = "Error";
            try
            {
                if (type.Contains("HANA"))
                {
                    //using (HanaConnection con = new HanaConnection(QueryAccess.HANA_conString(Server, User, Password, Database)))
                    //{
                    //    HanaCommand cmd = new HanaCommand();
                    //    cmd.CommandTimeout = 0;
                    //    cmd = con.CreateCommand();
                    //    con.Open();
                    //    cmd.CommandText = query;
                    //    cmd.ExecuteNonQuery();
                    //    ret = "";
                    //}
                }
                else
                {
                    using (SqlConnection con = new SqlConnection(QueryAccess.MSSQL_conString(Server, User, Password, Database)))
                    {
                        SqlCommand cmd = new SqlCommand();
                        cmd.CommandTimeout = 0;
                        cmd = con.CreateCommand();
                        con.Open();
                        cmd.CommandText = query;
                        cmd.ExecuteNonQuery();
                        ret = "";
                    }
                }
            }
            catch (Exception ex)
            {
                ret = $@"Error posting. Key: {DocEntry} " + ex.Message;
                //frmMain.NotiMsg(ex.Message, Color.Red);
            }
            return ret;
        }

        public static string ExecuteHana(string ConString, string query, string DocEntry)
        {
            string ret = "Error";
            try
            {

                //using (HanaConnection con = new HanaConnection(ConString))
                //{
                //    HanaCommand cmd = new HanaCommand();
                //    cmd = con.CreateCommand();
                //    con.Open();
                //    cmd.CommandText = query;
                //    cmd.ExecuteNonQuery();
                //    ret = "";
                //}
            }
            catch (Exception ex)
            {
                ret = $@"Error posting. Key: {DocEntry} " + ex.Message;
                //frmMain.NotiMsg(ex.Message, Color.Red);
            }
            return ret;
        }

        public static void SaveToFTP(MemoryStream stream, string FilePath, string user, string pass)
        {
            WebRequest request = WebRequest.Create(FilePath);
            request.Method = WebRequestMethods.Ftp.DeleteFile;
            request.Credentials = new NetworkCredential(user, pass);
            try
            {
                FtpWebResponse response = (FtpWebResponse)request.GetResponse();
            }
            catch (Exception ex)
            {

            }

            request.Abort();
            request = WebRequest.Create(FilePath);
            request.Method = WebRequestMethods.Ftp.UploadFile;
            request.Credentials = new NetworkCredential(user, pass);

            using (Stream ftpStream = request.GetRequestStream())
            {
                stream.WriteTo(ftpStream);

                ftpStream.Close();
            }
        }

        public static List<string> GetFileListFTP(string FilePath, string user, string pass)
        {
            try
            {
                WebRequest request = WebRequest.Create(FilePath);
                request.Method = WebRequestMethods.Ftp.ListDirectory;
                request.Credentials = new NetworkCredential(user, pass);
                FtpWebResponse response = (FtpWebResponse)request.GetResponse();
                Stream responseStream = response.GetResponseStream();
                StreamReader reader = new StreamReader(responseStream);
                string names = reader.ReadToEnd();

                reader.Close();
                response.Close();

                return names.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries).ToList();
            }
            catch (Exception)
            {
                throw;
            }
        }

        public static Stream ReadFromFTP(string FilePath, string user, string pass)
        {
            WebClient request = new WebClient();
            request.Credentials = new NetworkCredential(user, pass);

            byte[] newFileData = request.DownloadData(FilePath);
            MemoryStream stream = new MemoryStream(newFileData);

            return stream;

        }

        public string ConcatDecimal(string value)
        {
            if (value == "" || value == string.Empty || value == null)
            {
                return "0";
            }
            else if (value.Contains('.'))
            {
                var number = value.Split('.');
                var r = new Regex($@"^\d*[1-9]\d*$");
                if (r.IsMatch(number[1]))
                {
                    return string.Format(new NumberFormatInfo() { NumberDecimalDigits = 2 },
                        "{0:F}", new decimal(Convert.ToDouble(value))).ToString();
                }
                else
                {
                    return number[0];
                }
            }
            else
            {
                return value;
            }


        }

        public static DataSet GetExcelSheet(string filePath)
        {
            DataSet ds = new DataSet();
            using (xcel.Excel.XLWorkbook workBook = new xcel.Excel.XLWorkbook(filePath))
            {
                //Read the first Sheet from Excel file.
                var wscnt = workBook.Worksheets.Count;
                foreach (var workSheet in workBook.Worksheets)
                {
                    //Create a new DataTable.
                    DataTable dt = new DataTable();
                    dt.TableName = workSheet.Name.Replace(" ", "");
                    ds.Tables.Add(dt);
                }
                ds.AcceptChanges();
            }
            return ds;
        }
        public static DataSet GetExcelColumn(string filePath, ExcelMapperViewModel.ExcelMapperModel model)
        {
            DataSet ds = new DataSet();
            using (xcel.Excel.XLWorkbook workBook = new xcel.Excel.XLWorkbook(filePath))
            {
                //Read the first Sheet from Excel file.
                var wscnt = workBook.Worksheets.Count;
                foreach (var workSheet in workBook.Worksheets.Where(sel => sel.Name.Replace(" ", "") == model.Worksheet))//do this if you have many sheets???
                                                                                                                         //foreach (var workSheet in workBook.Worksheets)///I do this because there are only 1 sheets in the excel

                {
                    //Create a new DataTable.
                    DataTable dt = new DataTable();
                    dt.TableName = workSheet.Name.Replace(" ", "");
                    bool isHeader = false; //closeXCEL First Reading
                    int fcel = 0;
                    int lcel = 0;

                    var range = workSheet.RangeUsed();
                    var table = range.AsTable();
                    var fcell = table.Cells(c => c.Address.ToString() == model.HeaderRow).FirstOrDefault();

                    foreach (xcel.Excel.IXLRow row in workSheet.Rows($@"{fcell.Address.RowNumber}"))
                    {
                        fcel = row.Cells().First().Address.ColumnNumber;
                        lcel = row.Cells().Last().Address.ColumnNumber;
                        foreach (xcel.Excel.IXLCell cell in row.Cells())
                        {
                            //Validate if Row Name is the beginning from User Set
                            var celAddr = $@"{cell.Address.ColumnLetter}{cell.Address.RowNumber}";
                            if (model.HeaderRow == celAddr || isHeader == true)
                            {
                                dt.Columns.Add(cell.Value.ToString());
                                isHeader = true;
                                if (fcel == 0)
                                    fcel = cell.Address.ColumnNumber;

                            }
                        }

                        if (dt.Columns.Count > 0)
                        {
                            var drow = dt.NewRow();
                            dt.Rows.Add(drow);
                        }
                    }
                    ds.Tables.Add(dt);
                }
                ds.AcceptChanges();
            }
            return ds;
        }

        public static DataSet GetExcelData(string filePath, List<ExcelMapperViewModel.ExcelMapperModel> _model)
        {
            DataSet ds = new DataSet();
            using (xcel.Excel.XLWorkbook workBook = new xcel.Excel.XLWorkbook(filePath))
            {
                //Read the first Sheet from Excel file.
                var wscnt = workBook.Worksheets.Count;
                foreach (var model in _model)
                {
                    foreach (var workSheet in workBook.Worksheets.Where(sel => sel.Name.Replace(" ", "") == model.Worksheet))
                    {
                        //Create a new DataTable.
                        DataTable dt = new DataTable();
                        dt.TableName = workSheet.Name.Replace(" ", "");
                        bool isHeader = false; //closeXCEL First Reading
                        int fcel = 0;
                        int lcel = 0;
                        int colStart = 0;

                        var range = workSheet.RangeUsed();
                        var table = range.AsTable();
                        bool _rowStarted = false;

                        foreach (xcel.Excel.IXLRow row in workSheet.Rows())
                        {
                            //try
                            //{

                            //fcel = row.Cells().First().Address.ColumnNumber;
                            //}
                            //catch (Exception EX)
                            //{

                            //    throw;
                            //}
                            fcel = row.Cells().First().Address.ColumnNumber;
                            lcel = row.Cells().Last().Address.ColumnNumber;
                            if (row.Cells(c => c.Address.ToString() == model.HeaderRow).Any())
                            {
                                foreach (xcel.Excel.IXLCell cell in row.Cells())
                                {
                                    //Validate if Row Name is the beginning from User Set
                                    var celAddr = $@"{cell.Address.ColumnLetter}{cell.Address.RowNumber}";
                                    if (model.HeaderRow == celAddr || isHeader == true)
                                    {
                                        if (colStart == 0)
                                            colStart = cell.Address.ColumnNumber; //Set the Start of Column

                                        dt.Columns.Add(cell.Value.ToString());
                                        isHeader = true;
                                        if (fcel == 0)
                                            fcel = cell.Address.ColumnNumber;

                                    }
                                }
                            }

                            if (row.Cells(c => c.Address.ToString() == model.DataRowStart).Any())
                                _rowStarted = true;

                            if (_rowStarted)
                            {
                                if (dt.Columns.Count > 0)
                                {
                                    int rCnt = 0;
                                    var drow = dt.NewRow();
                                    foreach (xcel.Excel.IXLCell cell in row.Cells())
                                    {
                                        if (cell.Address.ColumnNumber >= colStart)
                                        {
                                            if (cell.Address.ColumnNumber <= dt.Columns.Count)
                                            {
                                                drow[rCnt] = cell.Value.ToString();
                                                rCnt++;
                                            }
                                            else
                                                break;
                                        }
                                    }
                                    dt.Rows.Add(drow);
                                }
                            }

                        }
                        ds.Tables.Add(dt);
                    } //End Foreach xCel-Sheets
                    ds.AcceptChanges();
                } //End Foreach model                
            }
            return ds;
        }
        public static Boolean OPSGetExcelData(string filePath, ExcelMapperViewModel.ExcelMapperModel model, int StartRw, string PrimaryKey, string TableName, string con, string Database, List<OPSFieldSets> ColumnNames, string Dest, out string errorMessage, int MapId, string EntityName, string ErrorPath)
        {
            Boolean _bool = false;
            errorMessage = "";
            try
            {
                DataTable dt = new DataTable();
                int rowcounter = 0;
                bool rowCounterTotal = true;
                int startRowNumber = 1;
                bool _rowStarted = false;
                bool _finalRow = true;
                while (_finalRow)
                {
                    bool start = true;
                    using (xcel.Excel.XLWorkbook workBook = new xcel.Excel.XLWorkbook(filePath))
                    {
                        if (start == false)
                        {

                            break;
                        }
                        //Read the first Sheet from Excel file.
                        var wscnt = workBook.Worksheets.Count;
                        //foreach (var model in _model)
                        //{

                        foreach (var workSheet in workBook.Worksheets.Where(sel => sel.Name.Replace(" ", "") == model.Worksheet))
                        //foreach (var workSheet in workBook.Worksheets)

                        {
                            if (start == false)
                            {
                                break;
                            }
                            using (DataSet ds = new DataSet())
                            {
                                //using (DataTable dt = new DataTable())
                                //{

                                //dt.Rows.Clear();
                                //Create a new DataTable.

                                dt.TableName = workSheet.Name.Replace(" ", "");
                                bool isHeader = false; //closeXCEL First Reading
                                int fcel = 0;
                                int lcel = 0;
                                int lrow = 0;
                                int colStart = 0;

                                var range = workSheet.RangeUsed();
                                var table = range.AsTable();
                                lrow = workSheet.LastRowUsed().RowNumber();

                                //if (rowcounter == lrow - 1)
                                //{
                                //    _finalRow = false;
                                //    break;
                                //}



                                var divident = lrow / 1000; ///10000 / 1000 = 10 loops


                                //var starting_row = i * 1000;

                                for (int rowIndex = startRowNumber; rowIndex <= workSheet.LastRowUsed().RowNumber(); rowIndex++)
                                {
                                    if (start == false)
                                    {
                                        break;
                                    }
                                    xcel.Excel.IXLRow row = workSheet.Row(rowIndex);

                                    fcel = row.Cells().First().Address.ColumnNumber;
                                    lcel = row.Cells().Last().Address.ColumnNumber;

                                    if (row.Cells(c => c.Address.ToString() == model.HeaderRow).Any())
                                    {
                                        foreach (xcel.Excel.IXLCell cell in row.Cells())
                                        {
                                            //Validate if Row Name is the beginning from User Set
                                            var celAddr = $@"{cell.Address.ColumnLetter}{cell.Address.RowNumber}";
                                            if (model.HeaderRow == celAddr || isHeader == true)
                                            {
                                                if (colStart == 0)
                                                    colStart = cell.Address.ColumnNumber; //Set the Start of Column

                                                dt.Columns.Add(cell.Value.ToString());
                                                isHeader = true;
                                                if (fcel == 0)
                                                    fcel = cell.Address.ColumnNumber;

                                            }
                                        }
                                    }

                                    if (row.Cells(c => c.Address.ToString() == model.DataRowStart).Any() || _rowStarted)
                                        _rowStarted = true;

                                    if (_rowStarted)
                                    {
                                        if (dt.Columns.Count > 0)
                                        {
                                            int rCnt = 0;
                                            var drow = dt.NewRow();
                                            foreach (xcel.Excel.IXLCell cell in row.CellsUsed(xcel.Excel.XLCellsUsedOptions.AllFormats))
                                            {

                                                if (cell.Address.ColumnNumber >= colStart)
                                                {
                                                    if (cell.Address.ColumnNumber >= colStart && cell.Address.ColumnNumber <= dt.Columns.Count)
                                                    {
                                                        string newCellValue = cell.Value?.ToString() ?? "";
                                                        drow[cell.Address.ColumnNumber - 1] = newCellValue;
                                                    }
                                                    else
                                                        break;
                                                }
                                            }
                                            dt.Rows.Add(drow);
                                            rowcounter++;

                                            if (rowcounter == lrow - 1 || dt.Rows.Count >= 1000)
                                            {

                                                startRowNumber = rowcounter + 2;
                                                ds.Tables.Add(dt);
                                                ds.AcceptChanges();



                                                var columnData = ds.Tables.Cast<DataTable>()
                                                   .SelectMany(xtable => xtable.Rows.Cast<DataRow>()
                                                                .Select(xrow => xtable.Columns.Cast<DataColumn>()

                                                                                              .ToDictionary(column => column.ColumnName,
                                                                                                            column => xrow[column])))
                                                     .ToList();



                                                var columnList = ds.Tables.Cast<DataTable>()
                                                  .Select(xtable => xtable.Columns.Cast<DataColumn>().ToList())
                                                  .ToList();

                                                dt.Rows.Clear();
                                                //dt = new DataTable();
                                                ds.Clear();


                                                QueryAccess.OPSCreateTable(PrimaryKey, TableName, con, Database, columnList, MapId);
                                                QueryAccess.OPSInsertData(TableName, con, Database, columnList, columnData, PrimaryKey, Dest, EntityName, MapId, ErrorPath);

                                                ds.Tables.Clear();
                                                if (rowcounter == lrow - 1)
                                                {
                                                    _finalRow = false;
                                                    start = false;
                                                    break;
                                                }


                                                //ds = new DataSet();
                                                //dt.TableName = workSheet.Name.Replace(" ", "");


                                                //foreach (var column in columnList)
                                                //{
                                                //    foreach (var xlist in column)
                                                //    {
                                                //        dt.Columns.Add(xlist.ToString());
                                                //    }
                                                //}


                                                start = false;


                                            }


                                        }
                                    }

                                    //break;

                                }
                                //break;  

                                //}//end of Data Table
                            }//end of Dataset

                        }//end of foreach worksheet


                    }//end of using worksheet

                }//end of using while loop

                return _bool = true;
            }
            catch (Exception ex)
            {

                errorMessage = ex.Message;

                return _bool;
            }

        }


        //sir Denzo Version
        //public static Boolean OPSGetExcelData(string filePath, ExcelMapperViewModel.ExcelMapperModel model, int StartRw, string PrimaryKey, string TableName, string con, string Database, List<OPSFieldSets> ColumnNames, string Dest, out string errorMessage, int MapId)
        //{
        //    Boolean _bool = false;
        //    errorMessage = "";
        //    try
        //    {
        //        //DataSet ds = new DataSet();
        //        using (xcel.Excel.XLWorkbook workBook = new xcel.Excel.XLWorkbook(filePath))
        //        {

        //            //Read the first Sheet from Excel file.
        //            var wscnt = workBook.Worksheets.Count;
        //            //foreach (var model in _model)
        //            //{

        //            foreach (var workSheet in workBook.Worksheets.Where(sel => sel.Name.Replace(" ", "") == model.Worksheet))
        //            //foreach (var workSheet in workBook.Worksheets)

        //            {
        //                ////Create a new DataTable.
        //                DataTable dt = new DataTable();
        //                dt.TableName = workSheet.Name.Replace(" ", "");
        //                bool isHeader = false; //closeXCEL First Reading
        //                int fcel = 0;
        //                int lcel = 0;
        //                int lrow = 0;
        //                int colStart = 0;

        //                var range = workSheet.RangeUsed();
        //                var table = range.AsTable();
        //                lrow = workSheet.LastRowUsed().RowNumber();
        //                int rowcounter = 0;
        //                int rowcontinu = 0;
        //                bool _rowStarted = false;

        //                double divrest = (double)lrow / 1000.0; ///10000 / 1000 = 10 loops
        //                var _divident = (divrest - Math.Truncate(divrest) > 0 ? divrest + 1 : divrest);
        //                int divident = (int)Math.Abs(double.Parse(_divident.ToString()));
        //                var lopcount = 0;

        //                //var starting_row = i * 1000;

        //                foreach (xcel.Excel.IXLRow row in workSheet.Rows())
        //                {

        //                    fcel = row.Cells().First().Address.ColumnNumber;
        //                    lcel = row.Cells().Last().Address.ColumnNumber;

        //                    if (row.Cells(c => c.Address.ToString() == model.HeaderRow).Any())
        //                    {
        //                        foreach (xcel.Excel.IXLCell cell in row.Cells())
        //                        {
        //                            //Validate if Row Name is the beginning from User Set
        //                            var celAddr = $@"{cell.Address.ColumnLetter}{cell.Address.RowNumber}";
        //                            if (model.HeaderRow == celAddr || isHeader == true)
        //                            {
        //                                if (colStart == 0)
        //                                    colStart = cell.Address.ColumnNumber; //Set the Start of Column

        //                                dt.Columns.Add(cell.Value.ToString());
        //                                isHeader = true;
        //                                if (fcel == 0)
        //                                    fcel = cell.Address.ColumnNumber;

        //                            }
        //                        }
        //                    }

        //                    if (row.Cells(c => c.Address.ToString() == model.DataRowStart).Any() || _rowStarted)
        //                        _rowStarted = true;

        //                    if (_rowStarted)
        //                    {
        //                        if (dt.Columns.Count > 0)
        //                        {
        //                            using (DataSet ds = new DataSet())
        //                            {
        //                                int rCnt = 0;
        //                                var drow = dt.NewRow();
        //                                foreach (xcel.Excel.IXLCell cell in row.CellsUsed(xcel.Excel.XLCellsUsedOptions.AllFormats))
        //                                {

        //                                    if (cell.Address.ColumnNumber >= colStart)
        //                                    {
        //                                        if (cell.Address.ColumnNumber >= colStart && cell.Address.ColumnNumber <= dt.Columns.Count)
        //                                        {
        //                                            string newCellValue = cell.Value?.ToString() ?? "";
        //                                            drow[cell.Address.ColumnNumber - colStart] = newCellValue;
        //                                        }
        //                                        else
        //                                            break;
        //                                    }
        //                                }
        //                                dt.Rows.Add(drow);

        //                                rowcounter++;
        //                                rowcontinu++;

        //                                if (rowcounter >= 1000 || ((lopcount == divident) && (rowcontinu == lrow - 1))) /// if (rowcounter == lrow - 1 || dt.Rows.Count >= 1000)
        //                                {
        //                                    lopcount++;
        //                                    rowcounter = 0; ///reset

        //                                    // DataTable dt = new DataTable();

        //                                    ds.Tables.Add(dt);
        //                                    ds.AcceptChanges();

        //                                    var columnData = ds.Tables.Cast<DataTable>()
        //                                       .SelectMany(xtable => xtable.Rows.Cast<DataRow>()
        //                                                    .Select(xrow => xtable.Columns.Cast<DataColumn>()

        //                                                                                  .ToDictionary(column => column.ColumnName,
        //                                                                                                column => xrow[column])))
        //                                         .ToList();



        //                                    var columnList = ds.Tables.Cast<DataTable>()
        //                                      .Select(xtable => xtable.Columns.Cast<DataColumn>().ToList())
        //                                      .ToList();

        //                                    //dt.clear();
        //                                    //dt = new datatable();

        //                                    ds.Clear();
        //                                    dt.Rows.Clear();
        //                                    dt.AcceptChanges();

        //                                    QueryAccess.OPSCreateTable(PrimaryKey, TableName, con, Database, columnList, MapId);
        //                                    QueryAccess.OPSInsertData(TableName, con, Database, columnList, columnData, PrimaryKey, Dest);

        //                                    //if (rowcounter == lrow - 1)
        //                                    //{
        //                                    //    break;
        //                                    //}


        //                                    //ds = new DataSet();

        //                                    //dt.TableName = workSheet.Name.Replace(" ", "");
        //                                    //foreach (var column in columnList)
        //                                    //{
        //                                    //    foreach (var xlist in column)
        //                                    //    {
        //                                    //        dt.Columns.Add(xlist.ToString());
        //                                    //    }
        //                                    //}



        //                                }//End 1000 row


        //                            }//End using
        //                        }//End Column Count
        //                    }//end rowStarted

        //                }//End ForEachRow

        //            }


        //        }
        //        return _bool = true;

        //    }
        //    catch (Exception ex)
        //    {

        //        errorMessage = ex.Message;

        //        return _bool;
        //    }

        //}





        public static string OPSCreateSuccessExcel(Dictionary<string, List<string>> SuccessListValue, string DBlocalcon, ExcelMapperViewModel.ExcelMapperModel listmode, int MapId, string LocalPath, out string errorMessage)
        {


            try
            {

                using (var workbook = new XLWorkbook())
                {

                    string tableName = $"{MapId}_{listmode.DestinationTable}";
                    string checkTableExistQuery = $@"IF OBJECT_ID('dbo.{tableName}', 'U') IS NOT NULL SELECT 1 ELSE SELECT 0";
                    var worksheet = workbook.Worksheets.Add(listmode.Worksheet); // Create a single worksheet for all data

                    using (var connection = new SqlConnection(DBlocalcon))
                    {
                        connection.Open();
                        using (var command = new SqlCommand(checkTableExistQuery, connection))
                        {
                            int tableExists = (int)command.ExecuteScalar();
                            if (tableExists == 1)
                            {

                                // Adding column headers
                                DataColumnCollection columns = null;
                                int rowIndex = 1;

                                foreach (var Success in SuccessListValue)
                                {
                                    //var MapId = Success[0];
                                    var _PrimaryKeyValue = Success.Key;
                                    var _PrimaryKey = Success.Value[0];

                                    string FetchDataHeader = $@"SELECT * from dbo.[{MapId}_{listmode.DestinationTable}] WHERE {_PrimaryKey} = '{_PrimaryKeyValue}'";


                                    // Fetch data from the database and store it in a DataTable
                                    DataTable dtResult = new DataTable();

                                    //using (var connection = new SqlConnection(DBlocalcon))
                                    // {
                                    using (var adapter = new SqlDataAdapter(FetchDataHeader, connection))
                                    {
                                        adapter.Fill(dtResult);
                                    }
                                    //}

                                    if (dtResult.Rows.Count > 0)
                                    {
                                        // Adding column headers only once, based on the first result
                                        if (columns == null)
                                        {
                                            columns = dtResult.Columns;

                                            for (int colIndex = 1; colIndex <= columns.Count - 2; colIndex++)
                                            {
                                                worksheet.Cell(rowIndex, colIndex).Value = columns[colIndex - 1].ColumnName;
                                            }

                                            worksheet.Cell(rowIndex, worksheet.LastColumnUsed().ColumnNumber() + 1).Value = "Status";

                                            rowIndex++;
                                        }

                                        // Adding data rows
                                        foreach (DataRow row in dtResult.Rows)
                                        {
                                            int columnsCounter = columns.Count - 2;
                                            for (int colIndex = 1; colIndex <= columnsCounter; colIndex++)
                                            {
                                                //worksheet.Cell(rowIndex, colIndex).Value = row[columns[colIndex - 1].ColumnName].ToString();

                                                var cellValue = row[columns[colIndex - 1].ColumnName].ToString();
                                                if (double.TryParse(cellValue, out double numericValue))
                                                {
                                                    // If the cell value can be parsed as a number, set the cell format to treat it as text.
                                                    worksheet.Cell(rowIndex, colIndex).SetValue(cellValue).Style.NumberFormat.Format = "@";
                                                }
                                                else
                                                {
                                                    // If the cell value is not a numeric value, simply set it as is.
                                                    worksheet.Cell(rowIndex, colIndex).Value = cellValue;
                                                }
                                            }

                                            worksheet.Cell(rowIndex, columnsCounter + 1).Value = "Uploaded Successfully";

                                            rowIndex++;
                                        }


                                    }
                                }

                                string DonePath = $"{LocalPath}\\Done\\{DateTime.Now.ToString("MM")}{DateTime.Now.ToString("yyyy")}\\{DateTime.Now.ToString("yyyyMMdd")}";
                                if (!Directory.Exists(DonePath))
                                {
                                    Directory.CreateDirectory(DonePath);
                                }

                                string filePath = $"{DonePath}\\{listmode.DestinationTable}_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";

                                workbook.SaveAs(filePath);




                                errorMessage = "";
                                return filePath;

                            }
                            else
                            {
                                // Handle the case where the table does not exist
                                return errorMessage = $"Table {tableName} Does not Exist";
                            }
                        }
                    }//using new sql


                }//end of using workbook
            }//end of try
            catch (Exception ex)
            {
                errorMessage = ex.Message;

                return errorMessage;
            }

        }



        public static string OPSCreateExcelData(string ErrorPath, Dictionary<string, List<string>> ErrrorValueList, string DBlocalcon, ExcelMapperViewModel.ExcelMapperModel listmode, int MapId, out string errorMessage)
        {

            try
            {
                using (var workbook = new XLWorkbook())
                {
                    var worksheet = workbook.Worksheets.Add(listmode.Worksheet); // Create a single worksheet for all data



                    // Adding column headers
                    DataColumnCollection columns = null;
                    int rowIndex = 1;

                    foreach (var Error in ErrrorValueList)
                    {
                        var _PrimaryKeyValue = Error.Key;
                        var _PrimaryKey = Error.Value[0];
                        var _ErrorMessage = Error.Value[1];

                        string FetchDataHeader = $@"SELECT * from dbo.[{MapId}_{listmode.DestinationTable}] WHERE {_PrimaryKey} = '{_PrimaryKeyValue}'";

                        // Fetch data from the database and store it in a DataTable
                        DataTable dtResult = new DataTable();
                        using (var connection = new SqlConnection(DBlocalcon))
                        {
                            using (var adapter = new SqlDataAdapter(FetchDataHeader, connection))
                            {
                                adapter.Fill(dtResult);
                            }
                        }

                        if (dtResult.Rows.Count > 0)
                        {
                            // Adding column headers only once, based on the first result
                            if (columns == null)
                            {
                                columns = dtResult.Columns;

                                for (int colIndex = 1; colIndex <= columns.Count - 2; colIndex++)
                                {
                                    worksheet.Cell(rowIndex, colIndex).Value = columns[colIndex - 1].ColumnName;
                                }

                                worksheet.Cell(rowIndex, worksheet.LastColumnUsed().ColumnNumber() + 1).Value = "Error Message";

                                rowIndex++;
                            }

                            // Adding data rows
                            foreach (DataRow row in dtResult.Rows)
                            {
                                int columnsCounter = columns.Count - 2;
                                for (int colIndex = 1; colIndex <= columnsCounter; colIndex++)
                                {
                                    //worksheet.Cell(rowIndex, colIndex).Value = row[columns[colIndex - 1].ColumnName].ToString();

                                    var cellValue = row[columns[colIndex - 1].ColumnName].ToString();
                                    if (double.TryParse(cellValue, out double numericValue))
                                    {
                                        // If the cell value can be parsed as a number, set the cell format to treat it as text.
                                        worksheet.Cell(rowIndex, colIndex).SetValue(cellValue).Style.NumberFormat.Format = "@";
                                    }
                                    else
                                    {
                                        // If the cell value is not a numeric value, simply set it as is.
                                        worksheet.Cell(rowIndex, colIndex).Value = cellValue;
                                    }
                                }

                                worksheet.Cell(rowIndex, columnsCounter + 1).Value = _ErrorMessage;


                                rowIndex++;
                            }


                        }
                    }

                    string CreateErrorPath = $"{ErrorPath}\\{DateTime.Now.ToString("MM")}{DateTime.Now.ToString("yyyy")}\\{DateTime.Now.ToString("yyyyMMdd")}";

                    if (!Directory.Exists(CreateErrorPath))
                    {
                        // Create the directory if it does not exist
                        Directory.CreateDirectory(CreateErrorPath);
                    }

                    string filePath = $"{CreateErrorPath}\\{listmode.DestinationTable}_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";

                    workbook.SaveAs(filePath);
                    errorMessage = "";

                    return filePath; // You can return the file path or any other information you may need.

                }

            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;

                return errorMessage;
            }



        }

        public static string OPSCreateExcelDuplicationChecker(string ErrorPath, Dictionary<string, List<string>> ErrrorValueList, string DBlocalcon, int MapId, string TableName)
        {


            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Sheet1"); // Create a single worksheet for all data

                // Adding column headers
                DataColumnCollection columns = null;
                int rowIndex = 1;

                foreach (var Error in ErrrorValueList)
                {
                    var _PrimaryKeyValue = Error.Key;
                    var _PrimaryKey = Error.Value[0];
                    var _ErrorMessage = Error.Value[1];

                    //string FetchDataHeader = $@"SELECT * from dbo.[{MapId}_Header] WHERE {_PrimaryKey} = '{_PrimaryKeyValue}'";
                    string FetchDataHeader = $@"SELECT * from dbo.[{TableName}] WHERE {_PrimaryKey} = '{_PrimaryKeyValue}'";

                    // Fetch data from the database and store it in a DataTable
                    DataTable dtResult = new DataTable();
                    using (var connection = new SqlConnection(DBlocalcon))
                    {
                        using (var adapter = new SqlDataAdapter(FetchDataHeader, connection))
                        {
                            adapter.Fill(dtResult);
                        }
                    }

                    if (dtResult.Rows.Count > 0)
                    {
                        // Adding column headers only once, based on the first result
                        if (columns == null)
                        {
                            columns = dtResult.Columns;





                            for (int colIndex = 1; colIndex <= columns.Count - 2; colIndex++)
                            {
                                worksheet.Cell(rowIndex, colIndex).Value = columns[colIndex - 1].ColumnName;
                            }

                            worksheet.Cell(rowIndex, worksheet.LastColumnUsed().ColumnNumber() + 1).Value = "Error Message";

                            rowIndex++;
                        }

                        // Adding data rows
                        foreach (DataRow row in dtResult.Rows)
                        {
                            int columnsCounter = columns.Count - 2;
                            for (int colIndex = 1; colIndex <= columnsCounter; colIndex++)
                            {
                                //worksheet.Cell(rowIndex, colIndex).Value = row[columns[colIndex - 1].ColumnName].ToString();

                                var cellValue = row[columns[colIndex - 1].ColumnName].ToString();
                                if (double.TryParse(cellValue, out double numericValue))
                                {
                                    // If the cell value can be parsed as a number, set the cell format to treat it as text.
                                    worksheet.Cell(rowIndex, colIndex).SetValue(cellValue).Style.NumberFormat.Format = "@";
                                }
                                else
                                {
                                    // If the cell value is not a numeric value, simply set it as is.
                                    worksheet.Cell(rowIndex, colIndex).Value = cellValue;
                                }
                            }

                            worksheet.Cell(rowIndex, columnsCounter + 1).Value = _ErrorMessage;


                            rowIndex++;
                        }


                    }
                }

                string _Tablename = TableName.Substring(TableName.IndexOf('_') + 1);

                string CreateDuplicateErrPath = $"{ErrorPath}\\{DateTime.Now.ToString("MM")}{DateTime.Now.ToString("yyyy")}\\{DateTime.Now.ToString("yyyyMMdd")}";

                if (!Directory.Exists(CreateDuplicateErrPath))
                {
                    // Create the directory if it does not exist
                    Directory.CreateDirectory(CreateDuplicateErrPath);
                }

                string filePath = $"{CreateDuplicateErrPath}\\{_Tablename}_Duplicate_Entry_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";

                workbook.SaveAs(filePath);
                return filePath; // You can return the file path or any other information you may need.
            }
        }

        public static DataSet ReadExcelData(string filePath, ExcelMapperViewModel map)
        {
            DataSet ds = new DataSet();
            using (xcel.Excel.XLWorkbook workBook = new xcel.Excel.XLWorkbook(filePath))
            {
                if (map != null)
                {
                    //Read the first Sheet from Excel file.
                    var wscnt = workBook.Worksheets.Count;
                    foreach (var workSheet in workBook.Worksheets)
                    {
                        //Create a new DataTable.
                        DataTable dt = new DataTable();
                        dt.TableName = workSheet.Name;
                        //Loop through the Worksheet rows.
                        bool firstRow = true;
                        foreach (xcel.Excel.IXLRow row in workSheet.Rows())
                        {
                            //Use the first row to add columns to DataTable.
                            if (firstRow)
                            {
                                foreach (xcel.Excel.IXLCell cell in row.Cells())
                                {
                                    dt.Columns.Add(cell.Value.ToString());
                                }
                                firstRow = false;
                            }
                            else
                            {
                                if (dt.Columns.Count > 0)
                                {
                                    int i = 0;
                                    if (workSheet.FirstColumnUsed(true) != null)
                                    {
                                        var fcel = workSheet.FirstColumnUsed().FirstCellUsed().Address.ColumnNumber;
                                        var lcel = workSheet.LastColumnUsed().FirstCellUsed().Address.ColumnNumber;
                                        //foreach (xcel.IXLCell cell in row.Cells(row.FirstCellUsed().Address.ColumnNumber, row.LastCellUsed().Address.ColumnNumber))
                                        bool IsEmptyRow = true;
                                        var drow = dt.NewRow();
                                        foreach (xcel.Excel.IXLCell cell in row.Cells())
                                        {
                                            drow[i] = cell.Value.ToString();
                                            //dt.Rows[dt.Rows.Count - 1][i] = cell.Value.ToString();
                                            IsEmptyRow = false;
                                            i++;
                                        }
                                        if (IsEmptyRow == false)
                                            dt.Rows.Add(drow); //Add rows to DataTable
                                    }

                                }
                            }
                        }
                        ds.Tables.Add(dt);
                    }
                }
            }
            return ds;
        }

        public static bool IsColumnExist(DataTable dt, string column)
        {
            try
            {
                var d = dt.AsEnumerable().Select(sel => (string)sel[$@"{column}"]).Any();
            }
            catch
            {
                return false;
            }
            return true;
        }
        #region XCEL_REFERENCE
        //Open the Excel file using ClosedXML.
        //Keep in mind the Excel file cannot be open when trying to read it
        //using (xcel.Excel.XLWorkbook workBook = new xcel.Excel.XLWorkbook(filePath))
        //{
        //    var wscnt = workBook.Worksheets.Count;
        //    foreach (var workSheet in workBook.Worksheets)
        //    {
        //        if (workSheet.FirstColumnUsed(true) != null)
        //        {
        //            var fcel = workSheet.FirstColumnUsed().FirstCellUsed().Address;
        //            var lcel = workSheet.LastColumnUsed().FirstCellUsed().Address;
        //            var ccl = workSheet.Cell(1, 1).DataType == xcel.Excel.XLDataType.Text ? workSheet.Cell(1, 1).Value.ToString() : "";
        //            if (!string.IsNullOrEmpty(ccl))
        //            {
        //                workSheet.Range(fcel, lcel).InsertRowsAbove(1);
        //            }
        //        }                              
        //    }
        //    workBook.Save();
        //}
        #endregion

        public static string GetConnectionString(string server, string database, string username, string password)
        {
            return $"Data Source={server};Initial Catalog={database};Persist Security Info=True;User ID={username};Password={password}";
        }
        public static string GetQueryString(string queryname)
        {
            var query = $"select QueryString from dbo.Queries where Code = '{queryname}'";
            return query;
        }

        public static string ChangepayrollPeriod(string input, string payroll_period)
        {
            // Define the keyword pattern
            string keywordPattern = "payperiod.name='";

            // Normalize the input to handle various spacing around '='
            string normalizedInput = Regex.Replace(input, @"\s*payperiod\.name\s*=\s*'", $"{keywordPattern}");

            // Find the position of the last occurrence of the normalized keyword pattern
            int lastStartKeyword = normalizedInput.LastIndexOf(keywordPattern);
            if (lastStartKeyword == -1)
            {
                // Keyword not found; return the original input
                return input;
            }

            // Calculate the starting index of the value to be extracted
            int lastStart = lastStartKeyword + keywordPattern.Length;

            // Find the position of the next single quote after the last occurrence of the keyword
            int lastEnd = normalizedInput.IndexOf("'", lastStart);
            if (lastEnd == -1 || lastEnd <= lastStart)
            {
                // End quote not found or invalid; return the original input
                return input;
            }

            // Extract the value between quotes
            string extractedString = normalizedInput.Substring(lastStart, lastEnd - lastStart);

            // Replace the extracted value with the new payroll_period
            input = input.Replace(extractedString, payroll_period);

            return input;
        }

        public  static async Task<string> CreateExcelReport(DataSet dataSets, string filepath,string payroll_period,string to_date, List<QuerySub> querySubs)
        {
            try
            {

                DateTime date = DateTime.Parse(to_date);
                // Format the DateTime object to the desired format
                string formattedDate = date.ToString("MMMM dd, yyyy");
                var filename = $"AMC Semi_Payroll Instruction_{formattedDate}.xlsx";

                string fullpath = $"{filepath}\\{filename}";

                using (var workbook = new XLWorkbook())
                {
                    
                    // Save the workbook

                    foreach (DataTable dataTable in dataSets.Tables)
                    {

                        //column
                        foreach (DataColumn column in dataTable.Columns)
                        {
                            string columnValue = column.ColumnName.ToString();

                            // Check for the presence of ":Data:" and remove everything from ":Data:" onward
                            int index = columnValue.IndexOf(":");
                            if (index != -1)
                            {
                                string newColumnName = columnValue.Substring(0, index).Trim();
                                column.ColumnName = newColumnName; 
                            }
                               
                        }
                        //rows
                        foreach (DataRow row in dataTable.Rows)
                        {
                            foreach (DataColumn column in dataTable.Columns)
                            {
                                if (column.DataType == typeof(DateTime) && row[column] != DBNull.Value)
                                {
                                    DateTime dateValue = (DateTime)row[column];
                                    if (dateValue < new DateTime(1900, 1, 1))
                                    {
                                        row[column] = new DateTime(1900, 1, 1); // Or replace with null or a default date
                                    }
                                }
                            }
                        }

                        if (dataTable.TableName.Contains("Link Box"))
                        {
                            dataTable.TableName = dataTable.TableName.Replace(" - Link Box", "");
                        }

                        var sheet_color = querySubs
                                    .Where(s => s.code.Contains(dataTable.TableName)) // Filter by `code` property containing `dataTable.TableName`
                                    .Select(s => s.Query.ToString())
                                    .FirstOrDefault();

                        if (sheet_color == null)
                        {
                            sheet_color = "#ADD8E6"; // Set a default color, e.g., white
                        }

                        if (!string.IsNullOrEmpty(dataTable.TableName) && dataTable.TableName.Length > 31)
                        {
                            dataTable.TableName = dataTable.TableName.Substring(0, 31);
                        }

                        var worksheet = workbook.Worksheets.Add(dataTable.TableName);

                        worksheet.Cell("A1").Value = "ALASKA MILK CORPORATION"; // Modify as needed
                        worksheet.Cell("A1").Style.Font.Bold = true; // Make A1 bold
                        worksheet.Cell("A2").Value = dataTable.TableName.ToUpper().ToString(); // Modify as needed
                        worksheet.Cell("A2").Style.Font.Bold = true; // Make A1 bold
                        worksheet.Cell("A3").Value = "Pay Period:"; // Modify as needed
                        worksheet.Cell("A3").Style.Font.Bold = true; // Make A1 bold
                        worksheet.Cell("B3").Value = payroll_period.ToUpper().ToString(); // Modify as needed
                        worksheet.Cell("B3").Style.Font.Bold = true; // Make A1 bold

                        int colHStart = 1; // Starting column index (H corresponds to column 1 in ClosedXML)
                        int rowHStart = 5; // Starting row index

                        // Loop through each DataColumn in the DataTable
                        foreach (DataColumn dtCol in dataTable.Columns)
                        {
                            long colIndex = colHStart; // Use fixed starting column

                            var headerCell = worksheet.Cell(rowHStart, (int)colIndex);
                            headerCell.Value = dtCol.ColumnName;

                            headerCell.Style.Font.Bold = true;
                            headerCell.Style.Fill.BackgroundColor = XLColor.FromHtml(sheet_color.ToString());// Change the hex color as needed
                            headerCell.Style.Font.FontName = "Tahoma"; // Set the font family to Tahoma
                            headerCell.Style.Font.FontSize = 10;
                            headerCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center; // Center horizontally
                            headerCell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                            worksheet.Row(rowHStart).Height = 30; // Set the height of the header row
                            // Increment the column index for the next column
                            colHStart++;
                        }
                        // Loop though each data rows
                        foreach (DataRow dtRow in dataTable.Rows)
                        {
                            colHStart = 1; //back to reset
                            foreach (DataColumn dtCol in dataTable.Columns)
                            {
                                var rowCell = worksheet.Cell(rowHStart + 1, colHStart);
                                rowCell.Value = dtRow[dtCol].ToString(); // Get the value from the DataRow
                                rowCell.Style.Font.FontName = "Arial"; // Change to "Arial" as per your request
                                rowCell.Style.Font.FontSize = 9; // Set the font size to 10
                                if (dtCol.ColumnName.Contains("Employee Name"))
                                {
                                    // Apply background color red to the header cell
                                    rowCell.Style.Fill.BackgroundColor = XLColor.LightCoral; // Set background color to red
                                    rowCell.Style.Font.FontColor = XLColor.Black; // Set background color to red
                                }
                                colHStart++;
                            }
                            rowHStart++;
                        }


                        int lastRow = dataTable.Rows.Count; // This gives you the number of rows in the DataTable
                        int lastColumn = dataTable.Columns.Count; // This gives you the number of columns in the DataTable


                        // If you need the column letter (e.g., A, B, C...):
                        string lastColumnLetter;

                        // Handling for columns A-Z (1-26)
                        if (lastColumn <= 26)
                        {
                            lastColumnLetter = ((char)(65 + lastColumn - 1)).ToString(); // A=65, B=66, ..., Z=90
                        }
                        else // Handling for columns beyond Z (AA, AB, etc.)
                        {
                            int dividend = lastColumn;
                            string columnLetter = String.Empty;
                            int modulo;

                            while (dividend > 0)
                            {
                                modulo = (dividend - 1) % 26; // Use (dividend - 1) for zero-based index
                                columnLetter = Convert.ToChar(modulo + 65) + columnLetter; // 65 is 'A'
                                dividend = (dividend - modulo) / 26; // Reduce the dividend
                            }

                            lastColumnLetter = columnLetter;
                        }

                        worksheet.Columns().AdjustToContents();
                        // Set the range for the whole DataTable for styling
                        workbook.Worksheet(dataTable.TableName);
                        //.Range($@"A1:{lastColumnLetter}{dataTable.Rows.Count + 4}") // Adjust the range according to your data
                        //.Style.Alignment.SetWrapText(false);
                        worksheet.Range($@"A5:{lastColumnLetter}{lastRow + 5}") // Data starts at row 5
                        .Style.Alignment.SetWrapText(false);

                        worksheet.Range($@"A5:{lastColumnLetter}{lastRow + 5}").Style
                            .Border.SetTopBorder(XLBorderStyleValues.Thin)
                            .Border.SetRightBorder(XLBorderStyleValues.Thin)
                            .Border.SetBottomBorder(XLBorderStyleValues.Thin)
                            .Border.SetLeftBorder(XLBorderStyleValues.Thin);

                        worksheet.TabColor = XLColor.FromHtml(sheet_color.ToString());

                        //workbook.Worksheet(dataTable.TableName).Range($@"A1:AA{dataTable.Rows.Count + 1}").Style.Alignment.SetWrapText(false);

                    }

                    if (!Directory.Exists(filepath))
                    {
                        // Create the directory if it does not exist
                        Directory.CreateDirectory(filepath);
                    }
                    workbook.SaveAs(fullpath);
                }

                return filename;
            }
            catch (Exception ex)
            {
                return $"error : - {ex.Message.ToString()}";
            }

        }

        public static async Task<byte[]> DownloadFileExcel(DataSet dataSets,string payroll_period,List<QuerySub> querySubs)
        {
            try
            {
                
                using (var workbook = new XLWorkbook())
                {
                    foreach (DataTable dataTable in dataSets.Tables)
                    {
                        foreach (DataColumn column in dataTable.Columns)
                        {
                            string columnValue = column.ColumnName.ToString();

                            // Check for the presence of ":Data:" and remove everything from ":Data:" onward
                            int index = columnValue.IndexOf(":");
                            if (index != -1)
                            {
                                string newColumnName = columnValue.Substring(0, index).Trim();
                                column.ColumnName = newColumnName;
                            }

                        }

                        foreach (DataRow row in dataTable.Rows)
                        {
                            foreach (DataColumn column in dataTable.Columns)
                            {
                                if (column.DataType == typeof(DateTime) && row[column] != DBNull.Value)
                                {
                                    DateTime dateValue = (DateTime)row[column];
                                    if (dateValue < new DateTime(1900, 1, 1))
                                    {
                                        row[column] = new DateTime(1900, 1, 1); // Or replace with null or a default date
                                    }
                                }
                            }
                        }
                        
                        if(dataTable.TableName.Contains("Link Box"))
                        {
                            dataTable.TableName = dataTable.TableName.Replace(" - Link Box", "");
                        }

                        var sheet_color = querySubs
                                    .Where(s => s.code.Contains(dataTable.TableName)) // Filter by `code` property containing `dataTable.TableName`
                                    .Select(s => s.Query.ToString())
                                    .FirstOrDefault();

                        if (sheet_color == null)
                        {
                            sheet_color = "#ADD8E6"; // Set a default color, e.g., white
                        }

                        if (!string.IsNullOrEmpty(dataTable.TableName) && dataTable.TableName.Length > 31)
                        {
                            dataTable.TableName = dataTable.TableName.Substring(0, 31);
                        }

                        var worksheet = workbook.Worksheets.Add(dataTable.TableName);

                        worksheet.Cell("A1").Value = "ALASKA MILK CORPORATION"; // Modify as needed
                        worksheet.Cell("A1").Style.Font.Bold = true; // Make A1 bold
                        worksheet.Cell("A2").Value = dataTable.TableName.ToUpper().ToString(); // Modify as needed
                        worksheet.Cell("A2").Style.Font.Bold = true; // Make A1 bold
                        worksheet.Cell("A3").Value = "Pay Period:"; // Modify as needed
                        worksheet.Cell("A3").Style.Font.Bold = true; // Make A1 bold
                        worksheet.Cell("B3").Value = payroll_period.ToUpper().ToString(); // Modify as needed
                        worksheet.Cell("B3").Style.Font.Bold = true; // Make A1 bold

                        int colHStart = 1; // Starting column index (H corresponds to column 1 in ClosedXML)
                        int rowHStart = 5; // Starting row index

                        // Loop through each DataColumn in the DataTable
                        foreach (DataColumn dtCol in dataTable.Columns)
                        {
                            long colIndex = colHStart; // Use fixed starting column

                            var headerCell = worksheet.Cell(rowHStart, (int)colIndex);
                            headerCell.Value = dtCol.ColumnName;

                            headerCell.Style.Font.Bold = true;
                            headerCell.Style.Fill.BackgroundColor = XLColor.FromHtml(sheet_color.ToString());// Change the hex color as needed
                            headerCell.Style.Font.FontName = "Tahoma"; // Set the font family to Tahoma
                            headerCell.Style.Font.FontSize = 10;
                            headerCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center; // Center horizontally
                            headerCell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                            worksheet.Row(rowHStart).Height = 30; // Set the height of the header row
                            // Increment the column index for the next column
                            colHStart++;
                        }
                        // Loop though each data rows
                        foreach(DataRow dtRow in dataTable.Rows)
                        {
                            colHStart = 1; //back to reset
                            foreach (DataColumn dtCol in dataTable.Columns)
                            {
                                var rowCell = worksheet.Cell(rowHStart + 1, colHStart);
                                rowCell.Value = dtRow[dtCol].ToString(); // Get the value from the DataRow
                                rowCell.Style.Font.FontName = "Arial"; // Change to "Arial" as per your request
                                rowCell.Style.Font.FontSize = 9; // Set the font size to 10
                                if (dtCol.ColumnName.Contains("Employee Name"))
                                {
                                    // Apply background color red to the header cell
                                    rowCell.Style.Fill.BackgroundColor = XLColor.LightCoral; // Set background color to red
                                    rowCell.Style.Font.FontColor = XLColor.Black; // Set background color to red
                                }
                                colHStart++;
                            }
                            rowHStart++;
                        }


                            int lastRow = dataTable.Rows.Count; // This gives you the number of rows in the DataTable
                            int lastColumn = dataTable.Columns.Count; // This gives you the number of columns in the DataTable


                        // If you need the column letter (e.g., A, B, C...):
                        string lastColumnLetter;

                        // Handling for columns A-Z (1-26)
                            if (lastColumn <= 26)
                            {
                                lastColumnLetter = ((char)(65 + lastColumn - 1)).ToString(); // A=65, B=66, ..., Z=90
                            }
                            else // Handling for columns beyond Z (AA, AB, etc.)
                            {
                                int dividend = lastColumn;
                                string columnLetter = String.Empty;
                                int modulo;

                                while (dividend > 0)
                                {
                                    modulo = (dividend - 1) % 26; // Use (dividend - 1) for zero-based index
                                    columnLetter = Convert.ToChar(modulo + 65) + columnLetter; // 65 is 'A'
                                    dividend = (dividend - modulo) / 26; // Reduce the dividend
                                }

                                lastColumnLetter = columnLetter;
                            }

                        worksheet.Columns().AdjustToContents();
                            // Set the range for the whole DataTable for styling
                            workbook.Worksheet(dataTable.TableName);
                            //.Range($@"A1:{lastColumnLetter}{dataTable.Rows.Count + 4}") // Adjust the range according to your data
                            //.Style.Alignment.SetWrapText(false);
                            worksheet.Range($@"A5:{lastColumnLetter}{lastRow + 5}") // Data starts at row 5
                            .Style.Alignment.SetWrapText(false);

                        worksheet.Range($@"A5:{lastColumnLetter}{lastRow + 5}").Style
                            .Border.SetTopBorder(XLBorderStyleValues.Thin)
                            .Border.SetRightBorder(XLBorderStyleValues.Thin)
                            .Border.SetBottomBorder(XLBorderStyleValues.Thin)
                            .Border.SetLeftBorder(XLBorderStyleValues.Thin);

                            worksheet.TabColor= XLColor.FromHtml(sheet_color.ToString());
                        


                        //workbook.Worksheets.Add(dataTable, dataTable.TableName);
                        //workbook.Worksheet(dataTable.TableName).Range($@"A1:AA{dataTable.Rows.Count + 1}").Style.Alignment.SetWrapText(false);
                    }
               
                    using (var stream = new MemoryStream())
                    {
                        workbook.SaveAs(stream);
                        // Ensure the stream is at the beginning before converting to byte array
                        stream.Position = 0;
                        return await Task.FromResult(stream.ToArray());
                    }
                  
                }
            }
            catch (Exception ex)
            {
                return null;
               
            }
        }

        public static string SetExcelPassword(string filePath, string password) //AMC Report version 8.1
        {
            try
            {
                if (!System.IO.File.Exists(filePath))
                {
                    throw new Exception("The specified file does not exist: " + filePath);
                }
                var excelApp = new Excel.Application();

                if (excelApp == null)
                {
                    throw new Exception(" Error 4403 Set Excel Password Failed to create an instance of Excel.Application.");
                }
                //Excel.Workbook workbook = null;
                //Excel.Workbook workbook = excelApp.Workbooks.Open(filePath);
                Excel.Workbook workbook = excelApp.Workbooks.Open(filePath);
                string messageret = "lock";
                try
                {
                    workbook.Password = password;
                    workbook.Save();
                }
                catch (Exception ex)
                {
                    
                    return $"Error Set Excel Password 4404: {ex.Message}";
                }
                finally
                {
                    workbook?.Close(false);

                    excelApp.Quit();

                    // Release COM objects
                    System.Runtime.InteropServices.Marshal.ReleaseComObject(workbook);
                    System.Runtime.InteropServices.Marshal.ReleaseComObject(excelApp);

                    //var excelProcesses = System.Diagnostics.Process.GetProcessesByName("EXCEL");
                    //foreach (var process in excelProcesses)
                    //{
                    //    process.Kill();
                    //}

                }

                return messageret;
            }
            catch(Exception e)
            {
                throw new Exception($"Error Set Excel Password 4405: {e.Message.ToString()}");
            }
          
        }
        public static string UnlockExcelFile(string filePath, string password)//AMC Report version 8.1
        {
            try
            {
                var excelApp = new Excel.Application();
                Excel.Workbook workbook = null;
                string messageret = "unlock";
                try
                {
                    // Open the workbook with the existing password
                    workbook = excelApp.Workbooks.Open(filePath, Password: password);

                    // Remove the password by setting it to an empty string
                    workbook.Password = "";
                    workbook.Save(); // Save changes
                }
                catch (System.Runtime.InteropServices.COMException ex)
                {
                    // Handle any errors such as wrong password
                    return $"Error: {ex.Message}";
                }
                finally
                {
                    // Close the workbook and quit the Excel application
                    workbook?.Close(false);
                    excelApp.Quit();

                    // Release COM objects
                    System.Runtime.InteropServices.Marshal.ReleaseComObject(workbook);
                    System.Runtime.InteropServices.Marshal.ReleaseComObject(excelApp);

                    //var excelProcesses = System.Diagnostics.Process.GetProcessesByName("EXCEL");
                    //foreach (var process in excelProcesses)
                    //{
                    //    process.Kill();
                    //}
                }

                return messageret;
            }
            catch(Exception e)
            {
                throw new Exception($"UnlockExcelFile : {e.Message.ToString()}");
            }
           
        }

        public static DataTable ExtractExcelReport(string LocalFile) // GAVIN
        {
            try
            {
                DataTable dt = new DataTable();
                //var csvFilePath = System.IO.Path.Combine(LocalPath, FileName);

                if (!File.Exists(LocalFile))
                {
                    return dt;
                    //throw new FileNotFoundException("The specified CSV file was not found.", csvFilePath);
                }

                using (TextFieldParser parser = new TextFieldParser(LocalFile))
                {
                    parser.TextFieldType = FieldType.Delimited;
                    parser.SetDelimiters(",");  // Set delimiter for CSV
                    parser.HasFieldsEnclosedInQuotes = true; // Handles commas inside values

                    if (!parser.EndOfData)
                    {
                        // Read header line
                        string[] headers = parser.ReadFields();
                        var columnNameCounts = new Dictionary<string, int>();

                        foreach (var header in headers)
                        {
                            string columnName = header.Trim();

                            // Ensure unique column names
                            if (columnNameCounts.ContainsKey(columnName))
                            {
                                columnNameCounts[columnName]++;
                                columnName = $"{columnName}_{columnNameCounts[columnName]}";
                            }
                            else
                            {
                                columnNameCounts[columnName] = 0;
                            }

                            dt.Columns.Add(columnName);
                        }
                    }

                    // Read data rows
                    while (!parser.EndOfData)
                    {
                        string[] fields = parser.ReadFields();

                        if (fields.Length == dt.Columns.Count)
                        {
                            dt.Rows.Add(fields);
                        }
                        else
                        {
                            throw new Exception($"Skipping row: {string.Join(",", fields)} (Column mismatch)");
                        }
                    }
                }

                return dt;
            }
            catch (Exception e)
            {
                //Console.WriteLine($"❌ Error: {e.Message}");
                throw;
            }
        }

        //public static DataSet GroupCSVByOrderType(string localpath, string filename)
        //{
        //    try
        //    {
        //        DataSet ds = new DataSet();
        //        var csvFilePath = System.IO.Path.Combine(localpath, filename);

        //        using (TextFieldParser parser = new TextFieldParser(csvFilePath, Encoding.UTF8))
        //        {
        //            parser.TextFieldType = FieldType.Delimited;
        //            parser.SetDelimiters(",");
        //            parser.HasFieldsEnclosedInQuotes = true;

        //            DataTable dt = new DataTable();

        //            // Step 1: Read CSV Headers
        //            string[] headers = parser.ReadFields();

        //            if (headers == null || headers.Length == 0)
        //                throw new InvalidOperationException("CSV file is empty or invalid.");

        //            // Filter only the columns we need
        //            var columnsToKeep = new[] { "Order date", "Date Processed", "payment method", "Transaction Amount/Handling fee", "Billing Statement", "Order Type" };

        //            var columnIndexes = headers.Select((header, index) => new { header = CleanData(header), index })
        //                                       .Where(x => columnsToKeep.Contains(x.header))
        //                                       .ToDictionary(x => x.header, x => x.index);

        //            // Add columns to DataTable
        //            foreach (var col in columnsToKeep)
        //            {
        //                dt.Columns.Add(col);
        //            }

        //            // Step 2: Read Data Rows
        //            while (!parser.EndOfData)
        //            {
        //                string[] fields = parser.ReadFields();

        //                if (fields.Length < columnIndexes.Count)
        //                    continue; // Skip incomplete rows

        //                var filteredFields = columnIndexes.Select(index => CleanData(fields[index.Value])).ToArray();
        //                dt.Rows.Add(filteredFields);
        //            }

        //            // Step 3: Group By 'Order Type' then 'Billing Statement'
        //            var groupedData = dt.AsEnumerable()
        //                                .GroupBy(row => new
        //                                {
        //                                    OrderType = row.Field<string>("Order Type"),
        //                                    BillingStatement = row.Field<string>("Billing Statement")
        //                                });

        //            // Step 4: Create DataTable for Each Group in DataSet
        //            foreach (var group in groupedData)
        //            {
        //                string orderType = string.IsNullOrWhiteSpace(group.Key.OrderType) ? "Unknown_OrderType" : group.Key.OrderType.Replace(" ", "_");
        //                string billing = string.IsNullOrWhiteSpace(group.Key.BillingStatement) ? "No_Billing" : group.Key.BillingStatement.Replace(" ", "_");

        //                string tableName = $"{orderType}_{billing}";

        //                if (ds.Tables.Contains(tableName))
        //                    tableName += "_Duplicate";

        //                DataTable groupTable = dt.Clone();
        //                foreach (var row in group)
        //                {
        //                    groupTable.ImportRow(row);
        //                }

        //                ds.Tables.Add(groupTable);
        //                ds.Tables[ds.Tables.Count - 1].TableName = tableName;
        //            }
        //        }

        //        return ds;
        //    }
        //    catch (Exception e)
        //    {
        //        Console.WriteLine("Error: " + e.Message);
        //        return null;
        //    }
        //}

        public static DataTable deposit_GroupCSVByOrderType(string localpath, string filename,Dictionary<int,string> ListOfAccounts)
        {
            try
            {
                var csvFilePath = System.IO.Path.Combine(localpath, filename);

                using (TextFieldParser parser = new TextFieldParser(csvFilePath, Encoding.UTF8))
                {
                    parser.TextFieldType = FieldType.Delimited;
                    parser.SetDelimiters(",");
                    parser.HasFieldsEnclosedInQuotes = true;

                    DataTable dt = new DataTable();

                    string[] headers = parser.ReadFields();
                    var columnsToKeep = new[] { "Order date", "Date Processed", "payment method", "Transaction Amount/Handling fee", "Billing Statement", "State", "Order Type"};

                    var columnIndexes = headers.Select((header, index) => new { header = CleanData(header), index })
                                               .Where(x => columnsToKeep.Contains(x.header))
                                               .ToDictionary(x => x.header, x => x.index);

                    foreach (var col in columnsToKeep)
                    {
                        dt.Columns.Add(col);
                    }

                    //while (!parser.EndOfData)
                    //{
                    //    string[] fields = parser.ReadFields();

                    //    if (fields.Length < columnIndexes.Count)
                    //        continue;

                    //    var filteredFields = columnIndexes.Select(index => CleanData(fields[index.Value])).ToArray();
                    //    dt.Rows.Add(filteredFields);
                    //}

                    while (!parser.EndOfData)
                    {
                        string[] fields = parser.ReadFields();

                        if (fields.Length < columnIndexes.Count)
                            continue;

                        var filteredFields = columnIndexes.Select(index =>
                        {
                            var value = CleanData(fields[index.Value]);

                            if (index.Key == "Billing Statement")
                            {
                                var lowerValue = value.ToLower();
                                var isPlatformOrder = CleanData(fields[columnIndexes["Order Type"]]).ToLower() == "platform order";

                                if (!ListOfAccounts.Any(account => lowerValue.Contains(account.Value.ToLower())) && isPlatformOrder)
                                {
                                    value = "Other Banks";
                                }
                            }

                            return value;
                        }).ToArray();

                        dt.Rows.Add(filteredFields);
                    }

                    //var groupedData = dt.AsEnumerable()
                    //            //.AsParallel()
                    //            .GroupBy(row => new
                    //            {
                    //                OrderType = row.Field<string>("Order Type"),
                    //                BillingStatement = row.Field<string>("Billing Statement")
                    //            })
                    //            .Select(group => new
                    //            {
                    //                OrderType = group.Key.OrderType,
                    //                BillingStatement = group.Key.BillingStatement,
                    //                FirstOrderDate = group.First().Field<string>("Order date"),
                    //                FirstDateProcessed = group.First().Field<string>("Date Processed"),
                    //                TotalAmount = group.Sum(row => double.TryParse(row.Field<string>("Transaction Amount/Handling fee"), out double value) ? value : 0),
                    //                PaymentMethods = string.Join(", ", group.Select(row => row.Field<string>("payment method")?.Replace("\n", " ").Replace("\r", " ")).Distinct()),
                    //                State = string.Join(", ", group.Select(row => row.Field<string>("State")?.Replace("\n", " ").Replace("\r", " ")).Distinct())

                    //            });


                    var groupedData = dt.AsEnumerable()
                        //.AsParallel()
                        .GroupBy(row => new
                        {
                            BillingStatement = row.Field<string>("Billing Statement")
                        })
                        .Select(group => new
                        {
                            BillingStatement = group.Key.BillingStatement,
                            FirstOrderDate = group.First().Field<string>("Order date"),
                            FirstDateProcessed = group.First().Field<string>("Date Processed"),
                            TotalAmount = group.Sum(row => double.TryParse(row.Field<string>("Transaction Amount/Handling fee"), out double value) ? value : 0),
                            OrderType = string.Join(", ", group.Select(row => row.Field<string>("Order Type")?.Replace("\n", " ").Replace("\r", " ")).Distinct()),
                            PaymentMethods = string.Join(", ", group.Select(row => row.Field<string>("payment method")?.Replace("\n", " ").Replace("\r", " ")).Distinct()),
                            State = string.Join(", ", group.Select(row => row.Field<string>("State")?.Replace("\n", " ").Replace("\r", " ")).Distinct())
                        });

                    DataTable resultTable = new DataTable();
                    resultTable.Columns.Add("Order date");
                    resultTable.Columns.Add("Date Processed");
                    resultTable.Columns.Add("payment method");
                    resultTable.Columns.Add("Transaction Amount/Handling fee");
                    resultTable.Columns.Add("Billing Statement");
                    resultTable.Columns.Add("State");
                    resultTable.Columns.Add("Order Type");


                    foreach (var item in groupedData)
                    {
                        resultTable.Rows.Add(item.FirstOrderDate, item.FirstDateProcessed, item.PaymentMethods, item.TotalAmount, item.BillingStatement, item.State, item.OrderType);
                    }

                    return resultTable;
                }
            }
            catch (Exception e)
            {
                return null;
            }
        }

        public static DataTable check_GroupCSVByOrderType(string localpath, string filename, Dictionary<int, string> ListOfAccounts)
        {
            try
            {
                var csvFilePath = System.IO.Path.Combine(localpath, filename);

                using (TextFieldParser parser = new TextFieldParser(csvFilePath, Encoding.UTF8))
                {
                    parser.TextFieldType = FieldType.Delimited;
                    parser.SetDelimiters(",");
                    parser.HasFieldsEnclosedInQuotes = true;

                    DataTable dt = new DataTable();

                    string[] headers = parser.ReadFields();
                    var columnsToKeep = new[] { "Time", "Risk control time", "Status", "Transaction Amount", "Payment method", "Payment channel", "Order Type" };

                    var columnIndexes = headers.Select((header, index) => new { header = CleanData(header), index })
                                               .Where(x => columnsToKeep.Contains(x.header))
                                               .ToDictionary(x => x.header, x => x.index);

                    foreach (var col in columnsToKeep)
                    {
                        dt.Columns.Add(col);
                    }

                    while (!parser.EndOfData)
                    {
                        string[] fields = parser.ReadFields();

                        if (fields.Length < columnIndexes.Count)
                            continue;

                        var filteredFields = columnIndexes.Select(index =>
                        {
                            var value = CleanData(fields[index.Value]);

                            if (index.Key == "Payment channel")
                            {
                                var lowerValue = value.ToLower();
                                var isPlatformOrder = CleanData(fields[columnIndexes["Order Type"]]).ToLower() == "platform order";

                                if (!ListOfAccounts.Any(account => lowerValue.Contains(account.Value.ToLower())) && isPlatformOrder)
                                {
                                    value = "Other Banks";
                                }
                            }

                            return value;
                        }).ToArray();

                        dt.Rows.Add(filteredFields);
                    }

                    //var groupedData = dt.AsEnumerable()
                    //            //.AsParallel()
                    //            .GroupBy(row => new
                    //            {
                    //                OrderType = row.Field<string>("Order Type"),
                    //                Paymentchannel = row.Field<string>("Payment channel")
                    //            })
                    //            .Select(group => new
                    //            {
                    //                OrderType = group.Key.OrderType,
                    //                PaymentChannel = group.Key.Paymentchannel,
                    //                Time = group.First().Field<string>("Time"),
                    //                RiskControlTime = group.First().Field<string>("Risk control time"),
                    //                TransactionAmount = group.Sum(row => double.TryParse(row.Field<string>("Transaction Amount"), out double value) ? value : 0),
                    //                PaymentMethods = string.Join(", ", group.Select(row => row.Field<string>("Payment method")?.Replace("\n", " ").Replace("\r", " ")).Distinct()),
                    //                Status = string.Join(", ", group.Select(row => row.Field<string>("Status")?.Replace("\n", " ").Replace("\r", " ")).Distinct())

                    //            });


                    var groupedData = dt.AsEnumerable()
                            //.AsParallel()
                            .GroupBy(row => new
                            {
                                Paymentchannel = row.Field<string>("Payment channel")
                            })
                            .Select(group => new
                            {
                                PaymentChannel = group.Key.Paymentchannel,
                                Time = group.First().Field<string>("Time"),
                                RiskControlTime = group.First().Field<string>("Risk control time"),
                                TransactionAmount = group.Sum(row => double.TryParse(row.Field<string>("Transaction Amount"), out double value) ? value : 0),
                                OrderType = string.Join(", ", group.Select(row => row.Field<string>("Order Type")?.Replace("\n", " ").Replace("\r", " ")).Distinct()),
                                PaymentMethods = string.Join(", ", group.Select(row => row.Field<string>("Payment method")?.Replace("\n", " ").Replace("\r", " ")).Distinct()),
                                Status = string.Join(", ", group.Select(row => row.Field<string>("Status")?.Replace("\n", " ").Replace("\r", " ")).Distinct())
                            });

                    DataTable resultTable = new DataTable();
                    resultTable.Columns.Add("Time");
                    resultTable.Columns.Add("Risk control time");
                    resultTable.Columns.Add("Status");
                    resultTable.Columns.Add("Transaction Amount");
                    resultTable.Columns.Add("Payment method");
                    resultTable.Columns.Add("Payment channel");
                    resultTable.Columns.Add("Order Type");


                    foreach (var item in groupedData)
                    {
                        resultTable.Rows.Add(item.Time, item.RiskControlTime, item.Status, item.TransactionAmount, item.PaymentMethods, item.PaymentChannel, item.OrderType);
                    }

                    return resultTable;
                }
            }
            catch (Exception e)
            {
                return null;
            }
        }


        private static string CleanData(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) 
                return string.Empty;

            if (value.Contains("export.userRech.orderType") || value.Contains("export.userWithdraw.orderType"))
                return value;

            if (DateTime.TryParse(value, out DateTime dateValue))
            {
                return dateValue.ToString("yyyy/MM/dd");
            }

            value = Regex.Replace(value, @"[^\u0000-\u00FF]+", "").Trim();
            value = value.Replace("\uFEFF", "").Replace("\u200B", "").Replace("?", "");

            // Handle currency format like "10,000.00(PHP)"
            var match = Regex.Match(value, @"([\d,]+\.?\d*)");
            if (match.Success)
            {
                string numberPart = match.Groups[1].Value.Replace(",", "");
                if (double.TryParse(numberPart, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out double result))
                {
                    return result.ToString("0.00");
                }
            }

            return value;
        }

        //private static string CleanData(string value)
        //{
        //    if (string.IsNullOrWhiteSpace(value)) return string.Empty;

        //    // ✅ Check if the value is a valid date
        //    if (DateTime.TryParse(value, out DateTime dateValue))
        //    {
        //        // ✅ Return in the format YYYY/MM/dd
        //        return dateValue.ToString("yyyy/MM/dd");
        //    }

        //    // ✅ Remove special characters and BOM
        //    value = Regex.Replace(value, @"[^\u0000-\u00FF]+", "").Trim();

        //    // ✅ Remove invisible characters and weird question marks
        //    value = value.Replace("\uFEFF", "").Replace("\u200B", "").Replace("?", "");

        //    // ✅ Extract the first numeric value (100.00)
        //    var match = Regex.Match(value, @"\d+(\.\d+)?");





        //    return match.Success ? match.Value : value;
        //}


        //for the version 2
        #region

        //public static DataTable GroupCSVByOrderType(string localpath, string filename)
        //{
        //    try
        //    {
        //        var csvFilePath = System.IO.Path.Combine(localpath, filename);

        //        List<dynamic> result = new List<dynamic>();

        //        using (TextFieldParser parser = new TextFieldParser(csvFilePath, Encoding.UTF8))
        //        {
        //            parser.TextFieldType = FieldType.Delimited;
        //            parser.SetDelimiters(",");
        //            parser.HasFieldsEnclosedInQuotes = true;

        //            string[] headers = parser.ReadFields();
        //            var columnsToKeep = new[] { "Order date", "Date Processed", "payment method", "Transaction Amount/Handling fee", "Billing Statement", "Order Type" };

        //            var columnIndexes = headers.Select((header, index) => new { header = CleanData(header), index })
        //                                       .Where(x => columnsToKeep.Contains(x.header))
        //                                       .ToDictionary(x => x.header, x => x.index);

        //            DataTable dt = new DataTable();

        //            foreach (var col in columnsToKeep)
        //            {
        //                dt.Columns.Add(col);
        //            }

        //            int batchSize = 100000;
        //            int rowCount = 0;

        //            while (!parser.EndOfData)
        //            {
        //                string[] fields = parser.ReadFields();

        //                if (fields.Length < columnIndexes.Count)
        //                    continue;

        //                var filteredFields = columnIndexes.Select(index => CleanData(fields[index.Value])).ToArray();
        //                dt.Rows.Add(filteredFields);
        //                rowCount++;

        //                // Process every 100,000 rows
        //                if (rowCount >= batchSize)
        //                {
        //                    result.AddRange(ProcessGroupData(dt));
        //                    dt.Clear();
        //                    rowCount = 0;
        //                    GC.Collect(); // Force garbage collection to free memory
        //                }
        //            }

        //            // Process remaining data
        //            if (dt.Rows.Count > 0)
        //            {
        //                result.AddRange(ProcessGroupData(dt));
        //            }
        //        }

        //        // Convert List to DataTable
        //        return ConvertToDataTable(result);
        //    }
        //    catch (Exception e)
        //    {
        //        return null;
        //    }
        //}

        //private static IEnumerable<dynamic> ProcessGroupData(DataTable dt)
        //{
        //    var groupedData = dt.AsEnumerable()
        //                        .GroupBy(row => new
        //                        {
        //                            OrderType = row.Field<string>("Order Type"),
        //                            BillingStatement = row.Field<string>("Billing Statement")
        //                        })
        //                        .Select(group => new
        //                        {
        //                            OrderType = group.Key.OrderType,
        //                            BillingStatement = group.Key.BillingStatement,
        //                            FirstOrderDate = group.First().Field<string>("Order date"),
        //                            FirstDateProcessed = group.First().Field<string>("Date Processed"),
        //                            TotalAmount = group.Sum(row => double.TryParse(row.Field<string>("Transaction Amount/Handling fee"), out double value) ? value : 0),
        //                            PaymentMethods = string.Join(", ", group.Select(row => row.Field<string>("payment method")).Distinct())
        //                        });

        //    return groupedData;
        //}

        //private static DataTable ConvertToDataTable(IEnumerable<dynamic> data)
        //{
        //    DataTable table = new DataTable();

        //    table.Columns.Add("Order Type");
        //    table.Columns.Add("Billing Statement");
        //    table.Columns.Add("Order date");
        //    table.Columns.Add("Date Processed");
        //    table.Columns.Add("Transaction Amount/Handling fee");
        //    table.Columns.Add("payment method");

        //    foreach (var item in data)
        //    {
        //        table.Rows.Add(item.OrderType, item.BillingStatement, item.FirstOrderDate, item.FirstDateProcessed, item.TotalAmount, item.PaymentMethods);
        //    }

        //    return table;
        //}

        #endregion

        public static void ConvertJsonAndResultsToExcel(Dictionary<string, string> jsonResultDict, string filePath)
        {
            if (jsonResultDict == null || jsonResultDict.Count == 0)
            {
                Console.WriteLine("The dictionary is null or contains no data. Exiting the method.");
                return;
            }

            // Use System.IO.Path explicitly to avoid ambiguity
            string directory = System.IO.Path.Combine(filePath, "Logs");
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            string fileName = $"Invoice_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
            string fullFilePath = System.IO.Path.Combine(directory, fileName);

            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Data");

                worksheet.Cell(1, 1).Value = "JSON";
                worksheet.Cell(1, 2).Value = "Result";

                int row = 2;
                foreach (var entry in jsonResultDict)
                {
                    worksheet.Cell(row, 1).Value = entry.Key;
                    worksheet.Cell(row, 2).Value = entry.Value;
                    row++;
                }

                workbook.SaveAs(fullFilePath);
            }

           // Console.WriteLine($"Excel file saved at: {System.IO.Path.GetFullPath(fullFilePath)}");
        }

        public static string[] PNGetExcelColumn(string Path, string ExcelFile)
        {
            //var getFileName = System.IO.Path.GetFileName(FileName);
            //var baseDirectory = System.IO.Path.GetDirectoryName(FileName);

            var Directory = System.IO.Path.Combine(Path, "Source");
            var prefix = ExcelFile;//.Split('_')[0];

            // Search the directory for a file that starts with the prefix.
            var sampleFile = System.IO.Directory.GetFiles(Directory, prefix + "*").FirstOrDefault();

            if (sampleFile == null)
            {
                throw new FileNotFoundException("No file starting with '" + prefix + "' was found in " + Directory);
            }
            var extension = System.IO.Path.GetExtension(sampleFile).ToLower();

            if (extension == ".csv")
            {
                // Read the first line of the CSV as column headers
                using (var reader = new StreamReader(sampleFile))
                {
                    var headerLine = reader.ReadLine();
                    if (headerLine == null)
                    {
                        throw new Exception("CSV file is empty: " + sampleFile);
                    }

                    // Split by comma (assuming standard CSV format)
                    return headerLine.Split(',').Select(column => column.Trim()).ToArray();
                }
            }
            else if (extension == ".xlsx" || extension == ".xls")
            {
                // Process as Excel file using ClosedXML
                using (var workbook = new XLWorkbook(sampleFile))
                {
                    var worksheet = workbook.Worksheet(1);
                    var headerRow = worksheet.FirstRowUsed();

                    if (headerRow == null)
                    {
                        throw new Exception("No header row found in the Excel file: " + sampleFile);
                    }

                    return headerRow.CellsUsed()
                        .Select(cell => cell.GetString().Trim())
                        .ToArray();
                }
            }
            else
            {
                throw new NotSupportedException("Unsupported file type: " + extension);
            }
          
        }

        public static string[] ReadTextFile(string path, string table)
        {
            var directory = System.IO.Path.Combine(path);
            string prefix = table.Equals("header", StringComparison.OrdinalIgnoreCase) ? "Header" : "Row";
            var sampleFile = Directory.GetFiles(directory, prefix + "*").FirstOrDefault();

            if (sampleFile == null)
            {
                throw new FileNotFoundException($"No file starting with '{prefix}' was found in {directory}");
            }

            string jsonContent = File.ReadAllText(sampleFile);

            // Use regex to extract column names (keys)
            var matches = Regex.Matches(jsonContent, @"""(.*?)""\s*:");

            HashSet<string> columnNames = new HashSet<string>();

            foreach (System.Text.RegularExpressions.Match match in matches)
            {
                if (match.Groups.Count > 1)
                {
                    columnNames.Add(match.Groups[1].Value);
                }
            }

            return columnNames.ToArray();
        }

        private static string FixMalformedJson(string json)
        {
            // 🔥 Fix missing quotes around property names
            json = Regex.Replace(json, @"(?<![""\w])(\w+)(?=\s*:)", "\"$1\"");

            // 🔥 Remove trailing commas before `}` or `]`
            json = Regex.Replace(json, @",\s*([\}\]])", "$1");

            return json;
        }

        public static string SaveDataTableToCsv(DataTable table, string folder,string ModuleName)
        {
            try
            {
                string logsFolder = System.IO.Path.Combine(folder, "Logs");

                if (!Directory.Exists(logsFolder))
                {
                    Directory.CreateDirectory(logsFolder);
                }

                //string filePath = System.IO.Path.Combine(logsFolder, $"{ModuleName}_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
                string filePath = System.IO.Path.Combine(logsFolder, $"{ModuleName.Replace("customtransaction_gvi_wthdrwal","withdrawal")}_{DateTime.Now:yyyyMMdd_HHmmss}.csv");

                using (var writer = new StreamWriter(filePath))
                using (var csv = new CsvWriter(writer))
                {
                    csv.Configuration.Delimiter = ",";

                    foreach (DataColumn column in table.Columns)
                    {
                        csv.WriteField(column.ColumnName);
                    }
                    csv.NextRecord();

                    foreach (DataRow row in table.Rows)
                    {
                        foreach (var value in row.ItemArray)
                        {
                            csv.WriteField(value?.ToString());
                        }
                        csv.NextRecord();
                    }
                }

                return filePath; // ✅ Return file path to verify it's created
            }
            catch (Exception ex)
            {
                return $"Error: {ex.Message}\nStackTrace: {ex.StackTrace}";
            }
        }



    }
}