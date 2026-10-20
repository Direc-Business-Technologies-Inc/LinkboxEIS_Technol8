using ExcelDataReader;
using DomainLayer.ViewModels;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;

namespace DataAccessLayer.Class
{
    public class ExcelAccess
    {
        public static Tuple<DataTable, DataTable> Getdata(string path, string BackupPath, string constr, string headtbname, string dbname, int id,
                                                          List<MapCreateViewModel.Header> HeaderField, List<MapCreateViewModel.Row> RowField)
        {
            string[] extensions = new[] { ".xls", ".xlsx" };

            int MaxTransId = QueryAccess.getTransId(constr, headtbname, dbname);
            DataTable tableheader = new DataTable();
            DataTable tablerow = new DataTable();
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            else
            {

                DirectoryInfo directory = new DirectoryInfo(path);
                FileInfo[] fi = directory.GetFiles().Where(f => extensions.Contains(f.Extension.ToLower())).ToArray();
                if (fi.Any() && fi.Length != 0)
                {
                    foreach (var filename in fi)
                    {

                        FileStream stream = File.Open($@"{path}\{filename.ToString()}", FileMode.Open, FileAccess.Read);

                        IExcelDataReader excelReader;
                        //1. Reading Excel file
                        if (Path.GetExtension($@"{path}\{filename.ToString()}").ToUpper() == ".XLS")
                        {
                            excelReader = ExcelReaderFactory.CreateBinaryReader(stream);
                        }
                        else
                        {
                            excelReader = ExcelReaderFactory.CreateOpenXmlReader(stream);
                        }

                        DataSet result = excelReader.AsDataSet(new ExcelDataSetConfiguration()
                        {
                            ConfigureDataTable = (_) => new ExcelDataTableConfiguration()
                            {
                                UseHeaderRow = true
                            }
                        });

                        if (!tableheader.Columns.Contains("TransactionId"))
                        {
                            tableheader.Columns.Add("TransactionId", typeof(System.String));
                            foreach (DataColumn col in result.Tables[1].Columns)
                            {
                                foreach (var item in HeaderField)
                                {
                                    if (item.DestinationField.ToString() == col.ToString())
                                    {
                                        tableheader.Columns.Add(col.ToString());
                                        break;
                                    }
                                }
                            }
                            //extra columns
                            tableheader.Columns.Add("UploadUserId", typeof(System.Int32));
                            tableheader.Columns.Add("UploadDate", typeof(System.DateTime));
                            tableheader.Columns.Add("Status", typeof(System.String));
                            tableheader.Columns.Add("Message", typeof(System.String));
                        }

                        if (!tablerow.Columns.Contains("TransactionId"))
                        {
                            tablerow.Columns.Add("TransactionId", typeof(System.Int32));

                            foreach (DataColumn col in result.Tables[0].Columns)
                            {
                                foreach (var item in RowField)
                                {
                                    if (item.AddonRowField.ToString() == col.ToString())
                                    {
                                        tablerow.Columns.Add(col.ToString());
                                        break;
                                    }
                                }
                            }

                        }

                        foreach (DataRow data in result.Tables[1].Rows)
                        {

                            object[] items = new object[tableheader.Columns.Count - 4];
                            var row = tableheader.NewRow();
                            for (int i = 0; i < data.ItemArray.Length - 1; i++)
                            {

                                if (tableheader.Columns.Contains(data.Table.Columns[i].ToString()))
                                {
                                    for (int count = 1; count < items.Length; count++)
                                    {
                                        if (items[count] is null)
                                        {
                                            items[count] = data[i];
                                            break;
                                        }
                                    }
                                    //items[i] = data[i].ToString();
                                }

                            }
                            row.ItemArray = (Object[])items;
                            row["TransactionId"] = MaxTransId;
                            row["UploadUserId"] = id;
                            row["UploadDate"] = DateTime.Now;
                            row["Status"] = "O";
                            tableheader.Rows.Add(row);

                            foreach (DataRow datarow in result.Tables[0].Rows)
                            {
                                object[] rowitems = new object[tablerow.Columns.Count];
                                var rows = tablerow.NewRow();
                                if (data["Branch_Code"].ToString() == datarow["Branch_Code"].ToString() &&
                                    data["TerminalID"].ToString() == datarow["TerminalID"].ToString() &&
                                    data["Cashier_Codes"].ToString() == datarow["Cashier_Code"].ToString())
                                {

                                    for (int i = 0; i < datarow.ItemArray.Length - 1; i++)
                                    {

                                        if (tablerow.Columns.Contains(datarow.Table.Columns[i].ToString()))
                                        {
                                            for (int count = 1; count < rowitems.Length; count++)
                                            {
                                                if (rowitems[count] is null)
                                                {
                                                    rowitems[count] = datarow[i];
                                                    break;
                                                }
                                            }
                                            //rowitems[i] = datarow[i].ToString();

                                        }

                                    }
                                    rows.ItemArray = (Object[])rowitems;

                                    rows["TransactionId"] = MaxTransId;
                                    tablerow.Rows.Add(rows);
                                }

                            }
                            MaxTransId++;
                        }

                        stream.Close();
                        File.Move($@"{path}\{filename.ToString()}", $@"{BackupPath}\{filename.ToString()}");
                    }
                }


            }

            return Tuple.Create(tableheader, tablerow);
        }
    }
}