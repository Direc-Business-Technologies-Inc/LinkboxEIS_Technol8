using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using System.Text;
using System.Web.Http;
using DataAccessLayer.Class;
using Newtonsoft.Json;
using DomainLayer.ViewModels;
using LinkBoxUI.Context;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using LinqToExcel.Extensions;
using System.Text.Json.Nodes;
using System.IO;
using System.Web.Helpers;
using DocumentFormat.OpenXml.Wordprocessing;
using System.Web.Mvc;
using NPOI.SS.Formula.Functions;
using DomainLayer.Models;
using DomainLayer;
using Newtonsoft.Json.Linq;
using StructureMap.Diagnostics;
using System.Globalization;
namespace LinkBoxUI.Services
{
    public class NetsuiteService
    {
        private readonly LinkboxDb _context = new LinkboxDb();
        NetsuiteHelper NShelper = new NetsuiteHelper();


        public async Task<string> NetsuitePost(string code)
        {
            try
            {
                //string FilePath = _context.PathSetup.Where(x => x.PathCode.Contains("ExcelPath")).Select(x => x.LocalPath).FirstOrDefault();

                //if (FilePath == null) throw new Exception("FilePath not Found");

                var modules = _context.Schedules
                .Join(_context.Process,  // ✅ Ensure correct DbSet names
                    schedule => schedule.Process,
                    process => process.ProcessCode,
                    (schedule, process) => new { schedule, process })
                .Join(_context.ProcessMap,
                    sp => sp.process.ProcessId,
                    processMap => processMap.ProcessId,
                    (sp, processMap) => new { sp.schedule, sp.process, processMap })
                .Join(_context.FieldMappings,
                    spm => spm.processMap.MapId,
                    fieldMapping => fieldMapping.MapId,
                    (spm, fieldMapping) => new { spm.schedule, spm.process, spm.processMap, fieldMapping })
                .Join(_context.PathSetup,  // ✅ New Join with PathSetup
                    fm => fm.fieldMapping.PathCode,  // ✅ Match PathCode
                    pathSetup => pathSetup.PathCode,
                    (fm, pathSetup) => new
                    {
                        fm.schedule.SchedCode,
                        MapId = fm.processMap.MapId,
                        ModuleName = fm.fieldMapping.ModuleName,
                        PathCode = fm.fieldMapping.PathCode,  // ✅ Include PathCode
                        LocalPath = pathSetup.LocalPath,
                        FileName  = fm.fieldMapping.FileName// ✅ Add related PathSetup data if needed
                    })
                .Where(x => x.SchedCode == code)  // ✅ Filtering by SchedCode
                .OrderBy(x=>x.MapId)
                .ToList();
                var ret_result = ""; 
                foreach (var module in modules)
                {
                
                    if (module.ModuleName.Equals("invoice"))
                    {
                        ret_result = await UploadSalesInvoice(module.LocalPath, module.FileName, code, module.MapId,module.ModuleName);

                    }
                    else
                    {
                        ret_result = await UploadTopUp(module.LocalPath, module.FileName, code, module.MapId,module.ModuleName);
                    }


                }

                return ret_result;

            }
            catch (Exception ex)
            {
                throw new Exception($"{ex.Message}{(ex.InnerException != null ? $" | Inner: {ex.InnerException.Message}" : "")}");
            }

        }

        public async Task <string> UploadTopUp(string LocalPath,string FileName,string code,int MapId,string ModuleName)
        {
            try
            {
                var QueryEndpoint = _context.APISetups.Where(x => x.APICode.Contains("NetsuiteFetchDataEndpoint")).Select(x => x.APIURL).FirstOrDefault();
                var PostDataEndpoint = _context.APISetups.Where(x => x.APICode.Contains("NetsuitePostEndpoint")).Select(x => x.APIURL).FirstOrDefault();


                var queryinput = "select id,fullname from account where accttype = 'Bank'";
                var accounts = await NShelper.GetDictionaryNetSuiteData(QueryEndpoint, queryinput);
                if (accounts.Count > 0 && accounts != null)
                {
                    var Headerfields = _context.Headers.Where(x => x.SourceTableName == "Header" && x.MapId == MapId).OrderBy(x => x.VisOrder).ToList();
                    var LineFields = _context.Headers.Where(x => x.SourceTableName == "Line" && x.MapId == MapId).OrderBy(x => x.VisOrder).ToList();
                    var Schedule = _context.Schedules.Where(x => x.SchedCode == code).FirstOrDefault();

                    

                    string original_folder = Path.Combine(LocalPath, "Original File");

                    if (!Directory.Exists(original_folder))
                    {
                        Directory.CreateDirectory(original_folder);
                    }

                   
                    string Uploading_folder = Path.Combine(LocalPath, "For Uploading");

                    if (!Directory.Exists(Uploading_folder))
                    {
                        Directory.CreateDirectory(Uploading_folder);
                    }

                    //string check_original_file = Path.Combine(LocalPath, FileName);

                     var check_original_file = Directory.GetFiles(LocalPath, "*.csv")
                             //.Where(f =>
                             //{
                             //    var filenameWithoutExt = Path.GetFileNameWithoutExtension(f);
                             //    return int.TryParse(filenameWithoutExt, out int number) && number >= 1 && number <= 9;
                             //})
                             .ToList();
                    //string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");

                    foreach (var orig_file in check_original_file)
                    {
                        //string fileNameOnly = Path.GetFileNameWithoutExtension(orig_file);
                        string fileNameWithExt = Path.GetFileName(orig_file);
                        DataTable DataResult;
                            if (ModuleName.Equals("deposit"))
                            {

                                DataResult = DataAccess.deposit_GroupCSVByOrderType(LocalPath, fileNameWithExt, accounts);
                            }
                            else
                            {
                                DataResult = DataAccess.check_GroupCSVByOrderType(LocalPath, fileNameWithExt, accounts);

                            }

                            if (!DataResult.Columns.Contains("Index No."))
                            {
                                DataResult.Columns.Add("Index No.", typeof(int));
                                int index = 1;

                                foreach (DataRow row in DataResult.Rows)
                                {
                                    row["Index No."] = index++;
                                }

                            }

                            if (!DataResult.Columns.Contains("Result"))
                                DataResult.Columns.Add("Result", typeof(string));

                            if (!DataResult.Columns.Contains("Posting Time"))
                                DataResult.Columns.Add("Posting Time", typeof(string));


                            //string new_FileName = $"{ModuleName.Replace("customtransaction_gvi_wthdrwal", "withdrawal")}_{timestamp}.csv";
                            //string ForUploadingTemplate = Path.Combine(Uploading_folder,$"{fileNameOnly}-{new_FileName}");

                        string ForUploadingTemplate = Path.Combine(Uploading_folder, fileNameWithExt);

                        SaveDataTableToCsv(DataResult, ForUploadingTemplate);

                        if (File.Exists(ForUploadingTemplate))
                        {
                            //string Original_backupName = $"{fileNameOnly}_backup_{timestamp}.csv";
                            //string newFilePath = Path.Combine(original_folder, Original_backupName);

                            string newFilePath = Path.Combine(original_folder, fileNameWithExt);
                            File.Move(orig_file, newFilePath);
                        }

                    }
                    



                    var matchingCsvFiles = Directory.GetFiles(Uploading_folder, "*.csv")
                    //.Where(f => Path.GetFileName(f).StartsWith(ModuleName.Replace("customtransaction_gvi_wthdrwal", "withdrawal") + "_", StringComparison.OrdinalIgnoreCase))
                    .ToList();

                    foreach (var file in matchingCsvFiles)
                    {
                        var DataResult = DataAccess.ExtractExcelReport(file);

                        
                        bool ToUploadItem = checkFileStatus(DataResult);

                        if (ToUploadItem)
                        {
                            foreach (DataRow item in DataResult.Rows)
                            {

                                string status = item["Result"]?.ToString();

                                if (string.Equals(status, "Success", StringComparison.OrdinalIgnoreCase))
                                {
                                    continue;
                                }

                                Schedule.RunTime = DateTime.Now;
                                _context.SaveChanges();
                                StringBuilder json = new StringBuilder();
                                try
                                {
                                    if (Headerfields != null)
                                    {
                                        json.AppendLine("{"); //Json Start
                                                              //var FieldSets = _context.Headers.Where(x => x.SourceTableName == "Header" && x.MapId == 17).ToList();

                                        foreach (var fields in Headerfields)
                                        {
                                            object fieldValue = null;
                                            switch (fields.SourceFieldId.Replace(" ", ""))
                                            {
                                                case "Query":
                                                    string input = fields.ConditionalQuery;
                                                    var output = Regex.Matches(input, $"#(.+?)#")
                                                                                        .Cast<System.Text.RegularExpressions.Match>()
                                                                                        .Select(m => m.Groups[1].Value);

                                                    foreach (var columnName in output)
                                                    {

                                                        //eRROR IF THE U_HBSCode is Not Exist in the SAP.. There is no Row at position 0 
                                                        input = QueryAccess.OriginalReplaceQueryParameter(input
                                                            , $"#{columnName}#"
                                                            , item[columnName].ToString(), null);

                                                        //string endpoint = "https://localhost:7242/api/Netsuite/QueryData";
                                                        // Define the query you want to send
                                                    }

                                                    fieldValue = await NShelper.GetNetSuiteData(QueryEndpoint, input);
                                                    if (fieldValue != null)
                                                    {
                                                        // Attempt to parse fieldValue as a number
                                                        if (int.TryParse(fieldValue.ToString(), out int intValue))
                                                        {
                                                            // If it's an integer, use intValue
                                                            fieldValue = intValue;
                                                        }
                                                        else if (double.TryParse(fieldValue.ToString(), out double doubleValue))
                                                        {
                                                            // If it's a decimal/float number, use doubleValue
                                                            fieldValue = doubleValue;
                                                        }

                                                        else
                                                        {
                                                            // Keep fieldValue as a string if it's not a number
                                                            fieldValue = fieldValue.ToString();
                                                        }
                                                    }

                                                    //fieldValue = "sample";

                                                    break;
                                                case "StaticValue":
                                                    fieldValue = fields.ConditionalQuery.ToString();
                                                    break;
                                                default:
                                                    fieldValue = item[fields.SourceFieldId.ToString()];
                                                    //fieldValue = item.Header.GetType()
                                                    // .GetProperty(fields.SourceFieldId.ToString().Replace(" ", ""))
                                                    // ?.GetValue(item.Header)?.ToString();

                                                    if (fieldValue != null && DateTime.TryParse(fieldValue.ToString(), out DateTime dateValue))
                                                    {
                                                        fieldValue = dateValue.ToString("yyyy-MM-dd");
                                                    }

                                                    break;
                                            }

                                            if (double.TryParse(fieldValue.ToString(), out double numericValue))
                                            {
                                                // If it's a valid number, append without quotes
                                                json.AppendLine($@"""{fields.DestinationField.ToString()}"":{numericValue},");
                                            }
                                            else if (DateTime.TryParse(fieldValue.ToString(), out DateTime _dateValue))
                                            {
                                                json.AppendLine($@"""{fields.DestinationField}"": ""{_dateValue:yyyy-MM-dd}"",");
                                            }
                                            else
                                            {
                                                // Otherwise, append with quotes
                                                json.AppendLine($@"""{fields.DestinationField.ToString()}"":""{fieldValue}"",");
                                            }

                                        }
                                    }


                                    #region
                                    if (LineFields != null)
                                    {
                                        string subtable = ModuleName == "deposit" ? "other" : "line";
                                        //json.AppendLine("\"other\": {");
                                        json.AppendLine("\"" + subtable + "\": {");
                                        json.AppendLine("\"items\": [");

                                        //var FieldSets_Line = _context.Headers.Where(x => x.SourceTableName == "Line" && x.MapId == 17).OrderBy(x => x.VisOrder).ToList();
                                        int totalItems = LineFields.Count(); // Total number of columns

                                        int currentIndex = 0; // Index to track the current column
                                        json.AppendLine("{"); // JSON start for items without Header
                                        foreach (var FieldLine in LineFields)
                                        {
                                            currentIndex++; // 
                                            object fielLinedValue = null;
                                            switch (FieldLine.SourceFieldId.Replace(" ", ""))
                                            {
                                                case "Query":


                                                    string input = FieldLine.ConditionalQuery;
                                                    var output = Regex.Matches(input, $"#(.+?)#")
                                                                                        .Cast<System.Text.RegularExpressions.Match>()
                                                                                        .Select(m => m.Groups[1].Value);

                                                    foreach (var columnName in output)
                                                    {
                                                        input = QueryAccess.OriginalReplaceQueryParameter(input
                                                            , $"#{columnName}#"
                                                            , item[columnName].ToString(), null);

                                                    }

                                                    fielLinedValue = await NShelper.GetNetSuiteData(QueryEndpoint, input);
                                                    if (fielLinedValue != null)
                                                    {
                                                        // Attempt to parse fieldValue as a number
                                                        if (int.TryParse(fielLinedValue.ToString(), out int intValue))
                                                        {
                                                            // If it's an integer, use intValue
                                                            fielLinedValue = intValue;
                                                        }
                                                        else if (double.TryParse(fielLinedValue.ToString(), out double doubleValue))
                                                        {
                                                            // If it's a decimal/float number, use doubleValue
                                                            fielLinedValue = doubleValue;
                                                        }
                                                        else
                                                        {
                                                            // Keep fieldValue as a string if it's not a number
                                                            fielLinedValue = fielLinedValue.ToString();
                                                        }
                                                    }



                                                    break;
                                                case "StaticValue":

                                                    fielLinedValue = FieldLine.ConditionalQuery.ToString();

                                                    break;

                                                default:

                                                    if (fielLinedValue != null && DateTime.TryParse(fielLinedValue.ToString(), out DateTime dateValue))
                                                    {
                                                        fielLinedValue = dateValue.ToString("yyyy-MM-dd");
                                                    }

                                                    fielLinedValue = item[FieldLine.SourceFieldId].ToString();

                                                    break;
                                            }

                                            if (FieldLine.DestinationField.Equals("id"))
                                            {
                                                json.AppendLine("\"item\":");
                                                json.AppendLine("{");

                                                // Check if the value is numeric (int, double, float, etc.)
                                                if (double.TryParse(fielLinedValue.ToString(), out double numericValue))
                                                {
                                                    // If it's a valid number, append it without quotes
                                                    json.AppendLine($"    \"id\": {numericValue}");
                                                }
                                                else
                                                {
                                                    // If it's not a number, keep it as a string with quotes
                                                    json.AppendLine($"    \"id\": \"{fielLinedValue}\"");
                                                }

                                                json.AppendLine("},");
                                            }
                                            else
                                            {
                                                //json.AppendLine($@"""{FieldLine.DestinationField.ToString()}"":""{fielLinedValue}"",");
                                                if (double.TryParse(fielLinedValue.ToString(), out double numericValue))
                                                {
                                                    // If it's a valid number, append without quotes
                                                    json.AppendLine($@"""{FieldLine.DestinationField.ToString()}"":{numericValue}");
                                                }
                                                else
                                                {
                                                    // If it's not a valid number, append with quotes
                                                    json.AppendLine($@"""{FieldLine.DestinationField.ToString()}"":""{fielLinedValue}""");
                                                }

                                                if (currentIndex < totalItems)
                                                {
                                                    // Add a comma if it's not the last column
                                                    json.AppendLine(",");
                                                }
                                                else
                                                {
                                                    // No comma for the last column
                                                    json.AppendLine();
                                                }

                                            }

                                        }
                                        //if (itemBPList.Value != 4)
                                        //{
                                        //    json.AppendLine("},"); //Json Start
                                        //}
                                        //else
                                        //{
                                        json.AppendLine("}"); //Json Start

                                        //}

                                    }
                                    #endregion

                                    json.AppendLine("]"); //Json Start
                                    json.AppendLine("}"); //Json Start

                                    json.AppendLine("}"); //Json Start

                                    //string RemoveItem = RemoveItemsWithoutAmount(json.ToString());
                                    string fixedJson = FixJson(json.ToString());
                                    string jsonStringFromBuilder = fixedJson.ToString();

                                    string Posting_result = await NShelper.PostDataToNetSuite(PostDataEndpoint, jsonStringFromBuilder, ModuleName);


                                    if (Posting_result.ToLower().Contains("successfully posted"))
                                    {
                                        item["Result"] = "Success";
                                        item["Posting Time"] = DateTime.Now.ToString();

                                    }
                                    else
                                    {
                                        
                                        item["Result"] = (Posting_result != null) ? Posting_result.ToString() : "Error Posting";

                                        string PostingDate = string.Empty;

                                        if (DataResult.Columns.Contains("Order date"))
                                        {
                                            PostingDate = item["Order date"]?.ToString();
                                        }
                                        else if (DataResult.Columns.Contains("Time"))
                                        {
                                            PostingDate = item["Time"]?.ToString();
                                        }

                                        string IndexNo = item["Index No."].ToString();

                                        string KeyError = $"{IndexNo}-{PostingDate}";

                                        var existingLog = _context.SystemLogs.FirstOrDefault(log =>
                                                    log.Table == KeyError &&
                                                    log.ErrorMsg == (Posting_result != null ? Posting_result.ToString() : "Error"));

                                        if (existingLog == null)
                                        {
                                            _context.SystemLogs.Add(new DomainLayer.Models.SystemLog
                                            {
                                                Task = "Netsuite Upload Error",
                                                CreateDate = DateTime.Now,
                                                ApiUrl = PostDataEndpoint,
                                                Module = ModuleName,
                                                Json = jsonStringFromBuilder.ToString(),
                                                Table = KeyError,
                                                ErrorMsg = (Posting_result != null ? Posting_result.ToString() : "Error")

                                            });

                                            _context.SaveChanges();
                                        }

                                    }



                                    //var newRow = LogTable.NewRow();
                                    //newRow["Message"] = Posting_result; // Or use actual response message
                                    //newRow["Posting Time"] = DateTime.Now;

                                    //for (int i = 0; i < item.ItemArray.Length; i++)
                                    //{
                                    //    newRow[i + 2] = item[i]; // Move original columns to the right
                                    //}

                                    //LogTable.Rows.Add(newRow);


                                    //jsonResultDict.Add(jsonStringFromBuilder, Posting_result);


                                }
                                catch (Exception ex)
                                {
                                    _context.SystemLogs.Add(new DomainLayer.Models.SystemLog { Task = "Upload Error", CreateDate = DateTime.Now, ApiUrl = $@"", Json = json.ToString(), ErrorMsg = $"{ex.InnerException?.Message}" });
                                    _context.SaveChanges();

                                }

                            }//end of mother Foreach

                            SaveDataTableToCsv(DataResult, file);

                            if (!checkFileStatus(DataResult))
                            {
                                string backupFolder = Path.Combine(LocalPath, "Backup");

                                string backupPath = Path.Combine(backupFolder, Path.GetFileName(file));

                                File.Move(file, backupPath);
                            }
                        }
                        else
                        {
                            string backupFolder = Path.Combine(LocalPath, "Backup");

                            string backupPath = Path.Combine(backupFolder, Path.GetFileName(file));

                            File.Move(file, backupPath);
                        }

                    }

                            //DataTable LogTable = new DataTable();

                            //LogTable = DataResult.Clone();
                            //// Insert new columns at the beginning
                            //if (!LogTable.Columns.Contains("Message"))
                            //    LogTable.Columns.Add("Message", typeof(string)).SetOrdinal(0);

                            //if (!LogTable.Columns.Contains("Posting Time"))
                            //    LogTable.Columns.Add("Posting Time", typeof(DateTime)).SetOrdinal(1);




                            





                            //if (LogTable != null && LogTable.Rows.Count > 0)
                            //{
                            //    var ex_message = DataAccess.SaveDataTableToCsv(LogTable, LocalPath, ModuleName);

                            //    if (ex_message.Contains("Error"))
                            //    {
                            //        _context.SystemLogs.Add(new DomainLayer.Models.SystemLog { Task = "SaveDataTableToCsv Message", CreateDate = DateTime.Now, ApiUrl = $@"", ErrorMsg = $"{ex_message}" });
                            //        _context.SaveChanges();
                            //    }

                            //}


                        }
                        return "Success";
            }
            catch(Exception e)
            {
                return e.Message;
            }
            
        }


        public async Task<string> UploadSalesInvoice(string LocalPath,string FileName, string code,int MapId,string ModuleName)
        {
            try
            {
                var QueryEndpoint = _context.APISetups.Where(x => x.APICode.Contains("NetsuiteFetchDataEndpoint")).Select(x => x.APIURL).FirstOrDefault();
                var PostDataEndpoint = _context.APISetups.Where(x => x.APICode.Contains("NetsuitePostEndpoint")).Select(x => x.APIURL).FirstOrDefault();
                var jsonResultDict = new Dictionary<string, string>();

                var Headerfields = _context.Headers.Where(x => x.SourceTableName == "Header" && x.MapId == MapId).OrderBy(x => x.VisOrder).ToList();
                var LineFields = _context.Headers.Where(x => x.SourceTableName == "Line" && x.MapId == MapId).OrderBy(x => x.VisOrder).ToList();


                var Schedule = _context.Schedules.Where(x => x.SchedCode == code).FirstOrDefault();


                string[] csvFiles = Directory.GetFiles(LocalPath, "*.csv");
                string forUploadingDir = Path.Combine(LocalPath, "For Uploading");

                if (csvFiles.Length > 0)
                {
                    foreach (string file in csvFiles)
                    {
                        string fileNameOnly = Path.GetFileName(file);
                        string SourceToForUploading = Path.Combine(forUploadingDir, fileNameOnly);


                        var lines = File.ReadAllLines(file);
                        var updatedLines = new List<string>();


                        bool headersExist = lines[0].Contains("Index No.") && lines[0].Contains("Status") && lines[0].Contains("Posting Time");

                        if (headersExist)
                        {
                            // Do not modify; just keep the original lines
                            updatedLines.AddRange(lines);
                        }
                        else
                        {
                            // Add headers
                            updatedLines.Add(lines[0] + ",Index No.,Result,Posting Time");

                            // Add Index No., leave Result and Posting Time blank
                            for (int i = 1; i < lines.Length; i++)
                            {
                                int indexNumber = i; // starts from 1
                                updatedLines.Add(lines[i] + $",{indexNumber},,");
                            }
                        }


                        // Overwrite the original file with updated content
                        File.WriteAllLines(file, updatedLines, Encoding.UTF8);

                        // Prepare target path

                        if (!Directory.Exists(forUploadingDir))
                        {
                            Directory.CreateDirectory(forUploadingDir);
                        }


                        File.Move(file, SourceToForUploading);

                    }
                }


                #region OLD Implementation
                //string check_csvFile = Path.Combine(LocalPath, FileName);
                //string forUploadingDir = Path.Combine(LocalPath, "For Uploading");
                //if (File.Exists(check_csvFile))
                //{
                //    string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");

                //    // Read and modify original file
                //    var lines = File.ReadAllLines(check_csvFile);
                //    var updatedLines = new List<string>();


                //    bool headersExist = lines[0].Contains("Index No.") && lines[0].Contains("Status") && lines[0].Contains("Posting Time");

                //    if (headersExist)
                //    {
                //        // Do not modify; just keep the original lines
                //        updatedLines.AddRange(lines);
                //    }
                //    else
                //    {
                //        // Add headers
                //        updatedLines.Add(lines[0] + ",Index No.,Result,Posting Time");

                //        // Add Index No., leave Result and Posting Time blank
                //        for (int i = 1; i < lines.Length; i++)
                //        {
                //            int indexNumber = i; // starts from 1
                //            updatedLines.Add(lines[i] + $",{indexNumber},,");
                //        }
                //    }


                //    // Overwrite the original file with updated content
                //    File.WriteAllLines(check_csvFile, updatedLines, Encoding.UTF8);

                //    // Prepare target path

                //    if (!Directory.Exists(forUploadingDir))
                //    {
                //        Directory.CreateDirectory(forUploadingDir);
                //    }

                //    string renamedFile = $"{ModuleName}_{timestamp}.csv";

                //    string destinationPath = Path.Combine(forUploadingDir, renamedFile);

                //    // Move and rename the modified file
                //    File.Move(check_csvFile, destinationPath);
                //}


                //var matchingCsvFiles = Directory.GetFiles(forUploadingDir, "*.csv")
                //     .Where(f => Path.GetFileName(f).StartsWith(ModuleName + "_", StringComparison.OrdinalIgnoreCase))
                //     .ToList();
                #endregion end of old impelementtion

                //After Moving All of the invoice Excel to For Uploading Folder
                //Uploading get All of the CSV file then Upload it
                var matchingCsvFiles = Directory.GetFiles(forUploadingDir, "*.csv").ToList();
                   
                foreach (var file in matchingCsvFiles)
                {
                    var DataResult = DataAccess.ExtractExcelReport(file);

                   // bool ToUploadItem = DataResult.AsEnumerable()
                   //.Any(row => !string.Equals(row.Field<string>("Status"), "Success", StringComparison.OrdinalIgnoreCase));
                    bool ToUploadItem = checkFileStatus(DataResult);

                    if (ToUploadItem)
                    {
                        // Clone the original structure without data
                        //DataTable LogTable = new DataTable();

                        //LogTable = DataResult.Clone();
                        //// Insert new columns at the beginning
                        //if (!LogTable.Columns.Contains("Message"))
                        //    LogTable.Columns.Add("Message", typeof(string)).SetOrdinal(0);

                        //if (!LogTable.Columns.Contains("Posting Time"))
                        //    LogTable.Columns.Add("Posting Time", typeof(DateTime)).SetOrdinal(1);

                        foreach (DataRow item in DataResult.Rows)
                        {
                            string status = item["Result"]?.ToString();

                            if (string.Equals(status, "Success", StringComparison.OrdinalIgnoreCase))
                            {
                                continue;
                            }

                                Schedule.RunTime = DateTime.Now;
                            _context.SaveChanges();
                            StringBuilder json = new StringBuilder();
                            try
                            {
                                if (Headerfields != null)
                                {
                                    json.AppendLine("{"); //Json Start
                                                          //var FieldSets = _context.Headers.Where(x => x.SourceTableName == "Header" && x.MapId == 17).ToList();

                                    foreach (var fields in Headerfields)
                                    {
                                        object fieldValue = null;
                                        switch (fields.SourceFieldId.Replace(" ", ""))
                                        {
                                            case "Query":
                                                string input = fields.ConditionalQuery;
                                                var output = Regex.Matches(input, $"#(.+?)#")
                                                                                    .Cast<System.Text.RegularExpressions.Match>()
                                                                                    .Select(m => m.Groups[1].Value);

                                                foreach (var columnName in output)
                                                {

                                                    //eRROR IF THE U_HBSCode is Not Exist in the SAP.. There is no Row at position 0 
                                                    input = QueryAccess.OriginalReplaceQueryParameter(input
                                                        , $"#{columnName}#"
                                                        , item[columnName].ToString(), null);

                                                    //string endpoint = "https://localhost:7242/api/Netsuite/QueryData";
                                                    // Define the query you want to send
                                                }

                                                fieldValue = await NShelper.GetNetSuiteData(QueryEndpoint, input);
                                                if (fieldValue != null)
                                                {
                                                    // Attempt to parse fieldValue as a number
                                                    if (int.TryParse(fieldValue.ToString(), out int intValue))
                                                    {
                                                        // If it's an integer, use intValue
                                                        fieldValue = intValue;
                                                    }
                                                    else if (double.TryParse(fieldValue.ToString(), out double doubleValue))
                                                    {
                                                        // If it's a decimal/float number, use doubleValue
                                                        fieldValue = doubleValue;
                                                    }

                                                    else
                                                    {
                                                        // Keep fieldValue as a string if it's not a number
                                                        fieldValue = fieldValue.ToString();
                                                    }
                                                }

                                                //fieldValue = "sample";

                                                break;
                                            case "StaticValue":
                                                fieldValue = fields.ConditionalQuery.ToString();
                                                break;
                                            default:
                                                fieldValue = item[fields.SourceFieldId.ToString()];
                                                //fieldValue = item.Header.GetType()
                                                // .GetProperty(fields.SourceFieldId.ToString().Replace(" ", ""))
                                                // ?.GetValue(item.Header)?.ToString();

                                                if (fieldValue != null && DateTime.TryParse(fieldValue.ToString(), out DateTime dateValue))
                                                {
                                                    fieldValue = dateValue.ToString("yyyy-MM-dd");
                                                }

                                                break;
                                        }

                                        if (double.TryParse(fieldValue.ToString(), out double numericValue))
                                        {
                                            // If it's a valid number, append without quotes
                                            json.AppendLine($@"""{fields.DestinationField.ToString()}"":{numericValue},");
                                        }
                                        else if (DateTime.TryParse(fieldValue.ToString(), new CultureInfo("en-US"), DateTimeStyles.None, out DateTime _dateValue))
                                        {
                                            json.AppendLine($@"""{fields.DestinationField}"": ""{_dateValue:yyyy-MM-dd}"",");
                                        }
                                        else
                                        {
                                            // Otherwise, append with quotes
                                            json.AppendLine($@"""{fields.DestinationField.ToString()}"":""{fieldValue}"",");
                                        }

                                    }
                                }


                                #region
                                if (LineFields != null)
                                {
                                    json.AppendLine("\"item\": {");
                                    json.AppendLine("\"items\": [");

                                    var itemBP_List = new Dictionary<string, int>
                                {
                                    { "TotalStake", 1 },    // As is
                                    { "TotalPayout", 2 },   // * -1
                                    { "TotalJackpot", 3 },  // * -1
                                    { "TotalPayment", 4 }   // Summation
                                };


                                    //var FieldSets_Line = _context.Headers.Where(x => x.SourceTableName == "Line" && x.MapId == 17).OrderBy(x => x.VisOrder).ToList();
                                    int totalItems = LineFields.Count(); // Total number of columns
                                    foreach (var itemBPList in itemBP_List)
                                    {
                                        int currentIndex = 0; // Index to track the current column
                                        json.AppendLine("{"); // JSON start for items without Header
                                        foreach (var FieldLine in LineFields)
                                        {
                                            currentIndex++; // 
                                            object fielLinedValue = null;
                                            switch (FieldLine.SourceFieldId.Replace(" ", ""))
                                            {
                                                case "Query":



                                                    int BPLIST_Value = 0;
                                                    if (FieldLine.DestinationField.Equals("id"))
                                                    {
                                                        BPLIST_Value = itemBPList.Value;
                                                    }



                                                    string input = FieldLine.ConditionalQuery;
                                                    var output = Regex.Matches(input, $"#(.+?)#")
                                                                                        .Cast<System.Text.RegularExpressions.Match>()
                                                                                        .Select(m => m.Groups[1].Value);

                                                    foreach (var columnName in output)
                                                    {
                                                        input = QueryAccess.OriginalReplaceQueryParameter(input
                                                            , $"#{columnName}#"
                                                            , item[columnName].ToString(), BPLIST_Value);

                                                    }

                                                    fielLinedValue = await NShelper.GetNetSuiteData(QueryEndpoint, input);
                                                    if (fielLinedValue != null)
                                                    {
                                                        // Attempt to parse fieldValue as a number
                                                        if (int.TryParse(fielLinedValue.ToString(), out int intValue))
                                                        {
                                                            // If it's an integer, use intValue
                                                            fielLinedValue = intValue;
                                                        }
                                                        else if (double.TryParse(fielLinedValue.ToString(), out double doubleValue))
                                                        {
                                                            // If it's a decimal/float number, use doubleValue
                                                            fielLinedValue = doubleValue;
                                                        }
                                                        else
                                                        {
                                                            // Keep fieldValue as a string if it's not a number
                                                            fielLinedValue = fielLinedValue.ToString();
                                                        }
                                                    }



                                                    break;
                                                case "StaticValue":
                                                    if (FieldLine.DestinationField.Equals("amount"))
                                                    {
                                                        if (itemBPList.Value == 4)
                                                        {
                                                            var paystake = item["Total stake"];//item.Lines.GetType().GetProperty("Totalstake")?.GetValue(item.Lines);
                                                            var payout = item["Total Payout"];//item.Lines.GetType().GetProperty("TotalPayout")?.GetValue(item.Lines);
                                                            var jackpot = item["Total Jackpot"];//item.Lines.GetType().GetProperty("TotalJackpot")?.GetValue(item.Lines);

                                                            // Ensure the correct type and apply the -1 multiplication where necessary
                                                            double paystakeValue = 0;
                                                            double payoutValue = 0;
                                                            double jackpotValue = 0;


                                                            if (double.TryParse(paystake.ToString(), out double stakeValue))
                                                            {
                                                                // Successfully parsed, now you can perform the computation
                                                                paystakeValue = stakeValue;
                                                            }

                                                            if (double.TryParse(payout.ToString(), out double _payoutValue))
                                                            {
                                                                // Successfully parsed, now you can perform the computation
                                                                payoutValue = _payoutValue * -1;
                                                            }

                                                            if (double.TryParse(jackpot.ToString(), out double _jackpotValue))
                                                            {
                                                                // Successfully parsed, now you can perform the computation
                                                                jackpotValue = _jackpotValue * -1;
                                                            }

                                                            // Perform the calculation
                                                            double _result = paystakeValue + payoutValue + jackpotValue;

                                                            // Assign to field value
                                                            fielLinedValue = _result * -1;
                                                        }
                                                        else
                                                        {
                                                            continue;
                                                        }

                                                    }
                                                    else
                                                    {
                                                        fielLinedValue = FieldLine.ConditionalQuery.ToString();
                                                    }
                                                    break;
                                                case "Totalstake":
                                                case "MX-CoStake":

                                                    object _paystake = 0;
                                                    if (FieldLine.SourceFieldId.Equals("Total stake"))
                                                    {
                                                        _paystake = item["Total stake"];

                                                    }
                                                    else
                                                    {
                                                        _paystake = item["MX - Co Stake"];

                                                    }

                                                    if (itemBPList.Value == 1)
                                                    {
                                                        //var _paystake = item.Lines.GetType().GetProperty("Totalstake")?.GetValue(item.Lines);
                                                        if (double.TryParse(_paystake.ToString(), out double stakeValue))
                                                        {
                                                            // Successfully parsed, now you can perform the computation
                                                            fielLinedValue = stakeValue;
                                                        }
                                                        else
                                                        {
                                                            // If parsing fails, you can handle the error or assign a default value
                                                            fielLinedValue = 0; // Default value if conversion fails
                                                        }

                                                        break;
                                                    }
                                                    continue;

                                                case "TotalPayout":
                                                case "MX-CoPayout":
                                                    object _payout = 0;

                                                    if (FieldLine.SourceFieldId.Equals("Total Payout"))
                                                    {

                                                        _payout = item["Total Payout"];

                                                    }
                                                    else
                                                    {
                                                        _payout = item["MX - Co Payout"];


                                                    }
                                                    if (itemBPList.Value == 2)
                                                    {
                                                        //var _payout = item.Lines.GetType().GetProperty("TotalPayout")?.GetValue(item.Lines);

                                                        if (double.TryParse(_payout.ToString(), out double payoutValue)) // && FieldLine.SourceFieldId.Equals("Total Payout")
                                                        {
                                                            // Successfully parsed, now you can perform the computation
                                                            fielLinedValue = payoutValue * -1;
                                                        }//else if(double.TryParse(_payout.ToString(), out double _payoutValue) && FieldLine.SourceFieldId.Equals("MX - Co Payout"))
                                                         //{
                                                         //    fielLinedValue = _payoutValue;
                                                         //}
                                                        else
                                                        {
                                                            // If parsing fails, you can handle the error or assign a default value
                                                            fielLinedValue = 0; // Default value if conversion fails
                                                        }
                                                        break;
                                                    }
                                                    continue;

                                                case "TotalJackpot":
                                                    if (itemBPList.Value == 3)
                                                    {
                                                        //var _jackpot = item.Lines.GetType().GetProperty("TotalJackpot")?.GetValue(item.Lines);
                                                        var _jackpot = item["Total Jackpot"];
                                                        if (double.TryParse(_jackpot.ToString(), out double jackpotValue))
                                                        {
                                                            // Successfully parsed, now you can perform the computation
                                                            fielLinedValue = jackpotValue * -1;
                                                        }
                                                        else
                                                        {
                                                            // If parsing fails, you can handle the error or assign a default value
                                                            fielLinedValue = 0; // Default value if conversion fails
                                                        }

                                                        break;
                                                    }
                                                    continue;
                                                case "ProfitorLoss":
                                                    if (itemBPList.Value == 4)
                                                    {
                                                        //var _jackpot = item.Lines.GetType().GetProperty("TotalJackpot")?.GetValue(item.Lines);
                                                        double payout = item["MX - Co Payout"] != null ? Convert.ToDouble(item["MX - Co Payout"]) : 0;
                                                        double stake = item["MX - Co Stake"] != null ? Convert.ToDouble(item["MX - Co Stake"]) : 0;
                                                        double _payment = Math.Round(payout - stake, 2); //item["Profit or Loss"];
                                                        if (double.TryParse(_payment.ToString(), out double _paymentValue))
                                                        {
                                                            // Successfully parsed, now you can perform the computation
                                                            fielLinedValue = _paymentValue;
                                                        }
                                                        else
                                                        {
                                                            // If parsing fails, you can handle the error or assign a default value
                                                            fielLinedValue = 0; // Default value if conversion fails
                                                        }

                                                        break;
                                                    }
                                                    continue;
                                                default:

                                                    if (fielLinedValue != null && DateTime.TryParse(fielLinedValue.ToString(), out DateTime dateValue))
                                                    {
                                                        fielLinedValue = dateValue.ToString("yyyy-MM-dd");
                                                    }

                                                    if (itemBPList.Value == 3)
                                                    {
                                                        fielLinedValue = 0;
                                                    }

                                                    fielLinedValue = item[FieldLine.SourceFieldId].ToString();
                                                    //    FieldLine.SourceFieldId.ToString().Replace(" ", "");
                                                    //fielLinedValue = item.Lines.GetType()
                                                    // .GetProperty(SourceField)
                                                    // ?.GetValue(item.Lines)?.ToString();



                                                    break;
                                            }

                                            if (FieldLine.DestinationField.Equals("id"))
                                            {
                                                json.AppendLine("\"item\":");
                                                json.AppendLine("{");

                                                // Check if the value is numeric (int, double, float, etc.)
                                                if (double.TryParse(fielLinedValue.ToString(), out double numericValue))
                                                {
                                                    // If it's a valid number, append it without quotes
                                                    json.AppendLine($"    \"id\": {numericValue}");
                                                }
                                                else
                                                {
                                                    // If it's not a number, keep it as a string with quotes
                                                    json.AppendLine($"    \"id\": \"{fielLinedValue}\"");
                                                }

                                                json.AppendLine("},");
                                            }
                                            else
                                            {
                                                //json.AppendLine($@"""{FieldLine.DestinationField.ToString()}"":""{fielLinedValue}"",");
                                                if (double.TryParse(fielLinedValue.ToString(), out double numericValue))
                                                {
                                                    // If it's a valid number, append without quotes
                                                    json.AppendLine($@"""{FieldLine.DestinationField.ToString()}"":{numericValue}");
                                                }
                                                else
                                                {
                                                    // If it's not a valid number, append with quotes
                                                    json.AppendLine($@"""{FieldLine.DestinationField.ToString()}"":""{fielLinedValue}""");
                                                }

                                                if (currentIndex < totalItems)
                                                {
                                                    // Add a comma if it's not the last column
                                                    json.AppendLine(",");
                                                }
                                                else
                                                {
                                                    // No comma for the last column
                                                    json.AppendLine();
                                                }

                                            }

                                        }
                                        if (itemBPList.Value != 4)
                                        {
                                            json.AppendLine("},"); //Json Start
                                        }
                                        else
                                        {
                                            json.AppendLine("}"); //Json Start

                                        }
                                    }

                                    //json.AppendLine("}"); //Json Start
                                }
                                #endregion

                                json.AppendLine("]"); //Json Start
                                json.AppendLine("}"); //Json Start

                                json.AppendLine("}"); //Json Start

                                string RemoveItem = RemoveItemsWithoutAmount(json.ToString());
                                string fixedJson = FixJson(RemoveItem.ToString());
                                string jsonStringFromBuilder = fixedJson.ToString();

                                string Posting_result = await NShelper.PostDataToNetSuite(PostDataEndpoint, jsonStringFromBuilder, ModuleName);
                                //var Posting_result = "200";

                                if (Posting_result.ToLower().Contains("successfully posted"))
                                {
                                    item["Result"] = "Success";
                                    item["Posting Time"] = DateTime.Now.ToString();

                                    //SaveDataTableToCsv(DataResult, file);
                                    //_context.SystemLogs.Add(new DomainLayer.Models.SystemLog
                                    //{
                                    //    Task = "Uploaded",
                                    //    CreateDate = DateTime.Now,
                                    //    ApiUrl = PostDataEndpoint,
                                    //    Module = ModuleName,
                                    //    Json = jsonStringFromBuilder.ToString(),
                                    //    ErrorMsg = $"{Posting_result} - Uploaded"
                                    //});


                                    //_context.SaveChanges();
                                }
                                else
                                {

                                    item["Result"] = (Posting_result != null) ? Posting_result.ToString() : "Error Posting";

                                    string PostingDate = string.Empty;

                                    if (DataResult.Columns.Contains("Settlement date"))
                                    {
                                        PostingDate = item["Settlement date"]?.ToString();
                                    }
                                    else if (DataResult.Columns.Contains("Date"))
                                    {
                                        PostingDate = item["Date"]?.ToString();
                                    }

                                    string IndexNo = item["Index No."].ToString();

                                    string KeyError = $"{IndexNo}-{PostingDate}";

                                    var existingLog = _context.SystemLogs.FirstOrDefault(log =>
                                                log.Table == KeyError &&
                                                log.ErrorMsg == (Posting_result != null ? Posting_result.ToString() : "Error"));

                                    if(existingLog == null)
                                    {
                                        _context.SystemLogs.Add(new DomainLayer.Models.SystemLog
                                        {
                                            Task = "Netsuite Upload Error",
                                            CreateDate = DateTime.Now,
                                            ApiUrl = PostDataEndpoint,
                                            Module = ModuleName,
                                            Json = jsonStringFromBuilder.ToString(),
                                            Table = KeyError,
                                            ErrorMsg = (Posting_result != null ? Posting_result.ToString() : "Error")

                                        });

                                        _context.SaveChanges();
                                    }
                                    
                                }

                                
                             
                            }
                            catch (Exception ex)
                            {
                                _context.SystemLogs.Add(new DomainLayer.Models.SystemLog { Task = "Upload Error", CreateDate = DateTime.Now, ApiUrl = $@"", Json = json.ToString(), ErrorMsg = $"{ex.InnerException?.Message}" });
                                _context.SaveChanges();

                            }



                        }//end of mother Foreach

                        SaveDataTableToCsv(DataResult, file);

                        if (!checkFileStatus(DataResult))
                        {
                            string backupFolder = Path.Combine(LocalPath, "Backup");

                            string backupPath = Path.Combine(backupFolder, Path.GetFileName(file));

                            File.Move(file, backupPath);
                        }

                        //Check if All of Them is UpLoaded


                        //if (LogTable != null && LogTable.Rows.Count > 0)
                        //{
                        //    var ex_message = DataAccess.SaveDataTableToCsv(LogTable, LocalPath, ModuleName);

                        //    if (ex_message.Contains("Error"))
                        //    {
                        //        _context.SystemLogs.Add(new DomainLayer.Models.SystemLog { Task = "SaveDataTableToCsv Message", CreateDate = DateTime.Now, ApiUrl = $@"", ErrorMsg = $"{ex_message}" });
                        //        _context.SaveChanges();
                        //    }

                        //}
                    }
                    else
                    {
                        string backupFolder = Path.Combine(LocalPath, "Backup");

                        string backupPath = Path.Combine(backupFolder, Path.GetFileName(file));

                        File.Move(file, backupPath);
                    }

                

                 

                }//end of Foreach



            }
            catch (Exception ex)
            {
                _context.SystemLogs.Add(new DomainLayer.Models.SystemLog { Task = "Task Error", CreateDate = DateTime.Now, ApiUrl = $@"", ErrorMsg = $"{ex.InnerException?.Message}" });
                _context.SaveChanges();
            }

            return "Success";
        }
        private void SaveDataTableToCsv(DataTable dt, string filePath)
        {
            var lines = new List<string>();

            // Add headers
            var columnNames = dt.Columns.Cast<DataColumn>()
                                        .Select(column => EscapeCsvField(column.ColumnName))
                                        .ToArray();
            lines.Add(string.Join(",", columnNames));

            // Add data rows
            foreach (DataRow row in dt.Rows)
            {
                var fields = row.ItemArray.Select(field => EscapeCsvField(field?.ToString() ?? string.Empty)).ToArray();
                lines.Add(string.Join(",", fields));
            }

            // Write to file (overwrite the existing file)
            File.WriteAllLines(filePath, lines, Encoding.UTF8);
        }
        private string EscapeCsvField(string field)
        {
            if (field.Contains(",") || field.Contains("\"") || field.Contains("\n") || field.Contains("\r"))
            {
                field = field.Replace("\"", "\"\""); // escape quotes by doubling
                return $"\"{field}\"";
            }
            return field;
        }

        private bool checkFileStatus(DataTable DataResult){
            bool ToUploadItem = DataResult.AsEnumerable()
                  .Any(row => !string.Equals(row.Field<string>("Result"), "Success", StringComparison.OrdinalIgnoreCase));

            return ToUploadItem;
        }

        static string FixJson(string json)
        {
            // Removes trailing commas before a closing bracket or brace
            return Regex.Replace(json, @",\s*([\]}])", "$1");
        }

        static string RemoveItemsWithoutAmount(string json)
        {
            try
            {
                JObject obj = JObject.Parse(json);
                JArray itemsArray = (JArray)obj["item"]["items"];

                // Remove objects that do not have an "amount" key
                itemsArray = new JArray(itemsArray.Where(item => item["amount"] != null));

                obj["item"]["items"] = itemsArray;
                return obj.ToString(Formatting.Indented);
            }
            catch (JsonException e)
            {
                Console.WriteLine("Invalid JSON: " + e.Message);
                return json;
            }
        }

    }
}
