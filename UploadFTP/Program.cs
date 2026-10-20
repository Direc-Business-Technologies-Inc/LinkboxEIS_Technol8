using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using DataAccessLayer.Class;

namespace UploadFTP
{
    class Program
    {
        static void Main(string[] args)
        {      
            //try
            //{
                
            //    List<string> CurrentTask = TaskScheduler.getRunningTask();
            //    foreach (var task in CurrentTask)
            //    {
            //        DataTable dtCreds = DataAccess.Select(Properties.Settings.Default.Constr, $"sp_getFieldCredentials {task.ToString()}");
            //        DataTable dtSyncCreds = DataAccess.Select(Properties.Settings.Default.Constr, $"sp_getSyncCredentials {task.ToString()}");
            //        DataTable dtEmailCreds = DataAccess.Select(Properties.Settings.Default.Constr, $"sp_getEmailCredentials {task.ToString()}");
            //        if (dtCreds != null && dtCreds.Rows.Count != 0)
            //        {
            //            DataTable dtHeaders = DataAccess.Select(Properties.Settings.Default.Constr, $"sp_getHeaderFields {Convert.ToInt32(dtCreds.Rows[0].ItemArray[0])}");
            //            DataTable dtRows = DataAccess.Select(Properties.Settings.Default.Constr, $"sp_getRowFields {Convert.ToInt32(dtCreds.Rows[0].ItemArray[0])}");
            //            List<DataRow> headList = dtHeaders.AsEnumerable().ToList();
            //            List<DataRow> rowList = dtRows.AsEnumerable().ToList();

            //            string constr = QueryAccess.con2(dtCreds.Rows[0].ItemArray[2].ToString(), 
            //                                             dtCreds.Rows[0].ItemArray[3].ToString(), 
            //                                             40000,
            //                                             dtCreds.Rows[0].ItemArray[5].ToString(), 
            //                                             dtCreds.Rows[0].ItemArray[6].ToString(), 
            //                                             dtCreds.Rows[0].ItemArray[7].ToString());

            //            var Data = ExcelAccessV2.Getdata(dtCreds.Rows[0].ItemArray[8].ToString(), 
            //                                             dtCreds.Rows[0].ItemArray[9].ToString(), 
            //                                             constr, 
            //                                             dtCreds.Rows[0].ItemArray[10].ToString(),                              
            //                                             dtCreds.Rows[0].ItemArray[2].ToString(), 
            //                                             headList, 
            //                                             rowList, 
            //                                             dtCreds.Rows[0].ItemArray[12].ToString(), 
            //                                             dtCreds.Rows[0].ItemArray[13].ToString(),
            //                                             dtCreds.Rows[0].ItemArray[14].ToString(),
            //                                             dtCreds.Rows[0].ItemArray[15].ToString());

            //            QueryAccess.UploadtoAddon(constr, 
            //                                      Data, 
            //                                      dtCreds.Rows[0].ItemArray[10].ToString(), 
            //                                      dtCreds.Rows[0].ItemArray[11].ToString());

            //            APIAccess.RunAsync().Wait();

            //        }
            //        if (dtSyncCreds != null && dtSyncCreds.Rows.Count != 0)
            //        {
            //            List<DataRow> list = dtSyncCreds.AsEnumerable().ToList();
            //            DataTable ExportData = SyncAccessV2.Execute(list);
            //            SyncAccessV2.ExportData(ExportData, list);

            //        }
            //        if (dtEmailCreds != null && dtEmailCreds.Rows.Count != 0)
            //        {
            //            string constr = QueryAccess.con2(dtEmailCreds.Rows[0].ItemArray[5].ToString(),
            //                                             dtEmailCreds.Rows[0].ItemArray[6].ToString(),
            //                                             40000,
            //                                             dtEmailCreds.Rows[0].ItemArray[7].ToString(), 
            //                                             dtEmailCreds.Rows[0].ItemArray[8].ToString(), 
            //                                             dtEmailCreds.Rows[0].ItemArray[9].ToString());

            //            DataTable StatusClosed = DataAccess.Select(Properties.Settings.Default.Constr, $"sp_getClosedItem {dtEmailCreds.Rows[0].ItemArray[5].ToString()}");
            //            DataTable StatusError = DataAccess.Select(Properties.Settings.Default.Constr, $"sp_getErrorItem {dtEmailCreds.Rows[0].ItemArray[5].ToString()}");
            //            EmailAccess.Send(StatusClosed, StatusError, dtEmailCreds);
            //        }
            //    }
            //}
            //catch (Exception ex)
            //{
            //    Console.WriteLine(ex);
            //}
        }
    }
}
