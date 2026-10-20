
using ClosedXML.Excel;
using DataAccessLayer.Class;
using DomainLayer;
using DomainLayer.Models;
using DomainLayer.ViewModels;
using LinkBoxUI.Context;
using LinkBoxUI.Helpers;
using Microsoft.Extensions.Logging;
using Microsoft.Graph.Models;
using Microsoft.Graph.Models.Security;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.Remoting.Metadata.W3cXsd2001;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web.UI.WebControls.Expressions;
using static LinkBoxUI.Services.Tiktok.TikTokSalesOrderServiceSAP;


namespace LinkBoxUI.Services.PrimaryAccounts
{
    public class PrimaryAcountServices
    {
        private readonly LinkboxDb _context = new LinkboxDb();
        public PostHelper phelper = new PostHelper();
        EmailSenderServiceHelper emailsent = new EmailSenderServiceHelper();
        public PostingViewModel OPSGetFileToSAPCredentialsMapCode(int mapid)
        {
            PostingViewModel model = new PostingViewModel();
            model.CredentialDetails = _context.FieldMappings.Where(x => x.MapId == mapid)

                        .GroupJoin(_context.PathSetup
                                    , fmap => fmap.PathCode
                                    , pset => pset.PathId.ToString()
                                    , (fmap, ps) => ps.Select(sel => new { fm = fmap, ps = sel }).DefaultIfEmpty(new { fm = fmap, ps = (PathSetup)null }))
                        .SelectMany(g => g)
                         .GroupJoin(_context.SAPSetup
                                    , fm => fm.fm.SAPCode
                                    , so => so.SAPCode
                                    , (fm, so) => so.Select(sel => new { fm = fm, so = sel }).DefaultIfEmpty(new { fm = fm, so = (SAPSetup)null }))
                        .SelectMany(g1 => g1)
                        .GroupJoin(_context.ModuleSetup
                                    , fmstd => fmstd.fm.fm.ModuleName
                                    , msd => msd.ModuleCode
                                    , (fmstd, msd) => msd.Select(sel => new { fmstd = fmstd, msd = sel }).DefaultIfEmpty(new { fmstd = fmstd, msd = (ModuleSetup)null }))
                        .SelectMany(g2 => g2).ToList()
                        .Select(f => new PostingViewModel.Credential
                        {
                            RowWorksheet = f?.fmstd?.fm?.fm?.RowWorksheet,
                            MapId = f?.fmstd?.fm?.fm?.MapId ?? 0,
                            MapName = f?.fmstd?.fm?.fm?.MapName ?? "",
                            Module = f?.fmstd?.fm?.fm?.ModuleName ?? "",

                            LocalPath = f?.fmstd?.fm?.ps?.LocalPath ?? "",
                            FTPPath = f?.fmstd?.fm?.ps?.RemotePath ?? "",
                            BackupPath = f?.fmstd?.fm?.ps?.BackupPath ?? "",
                            ErrorPath = f?.fmstd?.fm?.ps?.ErrorPath ?? "",

                            FileType = f?.fmstd?.fm?.fm?.FileType ?? "",
                            FileName = f?.fmstd?.fm?.fm?.FileName ?? "",

                            SAPDBName = f?.fmstd?.so?.SAPDBName ?? "",
                            SAPIPAddress = f?.fmstd?.so?.SAPIPAddress ?? "",
                            SAPSldServer = f?.fmstd?.so?.SAPSldServer ?? "",
                            SAPServerName = f?.fmstd?.so?.SAPServerName ?? "",
                            SAPLicensePort = f?.fmstd?.so?.SAPLicensePort ?? 0,

                            SAPDBuser = f?.fmstd?.so?.SAPDBuser ?? "",
                            SAPDBPassword = f?.fmstd?.so?.SAPDBPassword ?? "",
                            SAPUser = f?.fmstd?.so?.SAPUser ?? "",
                            SAPPassword = f?.fmstd?.so?.SAPPassword ?? "",

                            EntityType = f?.msd?.EntityType ?? "",
                            EntityName = f?.msd?.EntityName ?? "",
                            PrimaryKey = f?.msd?.PrimaryKey ?? "",
                        }).FirstOrDefault();
            return model;
        }
        public async Task<string> _UploadPrimaryAccount(string Code , int[] mapIds)
        {

            var sched = _context.Schedules.Where(sel => sel.SchedCode == Code).FirstOrDefault();

            foreach (var mapId in mapIds)
            {
               var Credentials = OPSGetFileToSAPCredentialsMapCode(mapId);

                var MapName = Credentials.CredentialDetails.MapName;


                string LocalPath = Credentials.CredentialDetails.LocalPath.ToString();
                var Module = Credentials.CredentialDetails.Module.ToString();

                if (string.IsNullOrWhiteSpace(LocalPath) || !Directory.Exists(LocalPath))
                {
                    Console.WriteLine($"LocalPath '{LocalPath}' is missing or invalid for MapId {mapId}, skipping.");
                    continue;
                }

                // get all of the csv or xlsx files in the local path
                var files = Directory.GetFiles(LocalPath, "*.*")
                    .Where(f => f.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase)
                             || f.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
                    .ToList();

                if (!files.Any())
                {
                    Console.WriteLine($"No csv/xlsx files found in '{LocalPath}' for MapId {mapId}.");
                    continue;
                }

                string donePath = Path.Combine(LocalPath, "Done");
                if (!Directory.Exists(donePath))
                {
                    Directory.CreateDirectory(donePath);
                }

                string errorPath = Credentials.CredentialDetails.ErrorPath?.ToString();
                if (!Directory.Exists(errorPath))
                {
                    Directory.CreateDirectory(errorPath);
                }// adjust property name if different
                string backupPath = Credentials.CredentialDetails.BackupPath.ToString();

                if (!Directory.Exists(backupPath))
                {
                    Directory.CreateDirectory(backupPath);
                }




                foreach (var filePath in files)
                {
                    var excelFileName = Path.GetFileName(filePath);
                    var safeFileName = excelFileName.Replace("'", "''"); // escape single quotes for SQL literal

                    string file_input_qrycmd = $@"
                        SELECT TOP 1 1
                        FROM dbo.ORDR
                        WHERE U_Template_Name IS NOT NULL
                          AND LEFT(U_Template_Name, LEN(U_Template_Name) - CHARINDEX('.', REVERSE(U_Template_Name)))
                              = LEFT('{safeFileName}', LEN('{safeFileName}') - CHARINDEX('.', REVERSE('{safeFileName}')));";


                    var connectionString = DataAccess.GetConnectionString(
                       Credentials.CredentialDetails.SAPIPAddress,
                       Credentials.CredentialDetails.SAPDBName,
                       Credentials.CredentialDetails.SAPDBuser,
                       Credentials.CredentialDetails.SAPDBPassword);

                    var dtResult = DataAccess.Select(connectionString, file_input_qrycmd);

                    bool isDuplicate = dtResult != null && dtResult.Rows.Count > 0;

                    if (isDuplicate)
                    {
                        _context.SystemLogs.Add(new DomainLayer.Models.SystemLog
                        {
                            Task = "Excel to SAP",
                            CreateDate = DateTime.Now,
                            ErrorMsg = $"File {excelFileName} already exists in SAP, skipping upload.",
                            Database = Credentials.CredentialDetails.SAPDBName.ToString(),
                            Module = excelFileName,
                            Table = Credentials.CredentialDetails.Module
                        });
                        _context.SaveChanges();

                        try
                        {
                            var duplicateDir = Path.Combine(errorPath, "Duplicate");
                            Directory.CreateDirectory(duplicateDir);
                            var duplicatePath = Path.Combine(duplicateDir, excelFileName);

                            if (File.Exists(duplicatePath))
                                File.Delete(duplicatePath);

                            File.Move(filePath, duplicatePath);
                        }
                        catch (Exception ex)
                        {
                            throw new Exception($"Failed to move '{filePath}' to backup folder.", ex);
                        }
                        continue;
                    }


                    string extension = Path.GetExtension(filePath).ToLowerInvariant();

                    List<IGrouping<string, List<object>>> grouped = null;

                    //var grouped = ReadExcelGroupedByColumnB(FileDirectory);

                    var isGroupByColumn = _context.Headers.Where(x => x.IsKeyValue && x.MapId == mapId).Select(x => x.SourceFieldId).FirstOrDefault();
                    string rowStart = Credentials.CredentialDetails.RowWorksheet?.ToString();

                    if (!int.TryParse(rowStart, out int startRow) || startRow < 1)
                    {
                        Console.WriteLine($"Invalid or missing RowWorksheet '{rowStart}' for MapId {mapId}, defaulting to row 1.");
                        startRow = 1;
                    }

                    if (extension == ".xlsx")
                    {
                        grouped = await ReadXlsxGroupedByColumn(filePath, isGroupByColumn.ToString(), Module, MapName, startRow);
                    }
                    else if (extension == ".csv")
                    {
                        // TODO: confirm which column letter to group by for CSV (hardcoding "B" for now to match the Excel logic)
                        grouped = await ReadCsvGroupedByColumn(filePath, isGroupByColumn.ToString(),Module,MapName, startRow);
                    }

                    if (grouped == null)
                    {
                        Console.WriteLine($"Could not read or group file '{filePath}', skipping.");
                        continue;
                    }

                    var successRows = new List<SyncResultRow>();
                    var errorRows = new List<SyncResultRow>();

                    foreach (var group in grouped) // grouped total
                    {
                        sched.IsRunning = true;
                        sched.RunTime = DateTime.Now;
                        _context.SaveChanges();

                        var payload = new JArray();
                        Console.WriteLine($"B = {group.Key} ({group.Count()} rows)");

                        var new_GroupKey = $"[{MapName}] - {group.Key}";


                        if (string.IsNullOrEmpty(group.Key))
                        {
                            _context.SystemLogs.Add(new DomainLayer.Models.SystemLog
                            {
                                Task = "Excel to SAP",
                                CreateDate = DateTime.Now,
                                ErrorMsg = $"ReferenceId number is missing.",
                                Database = Credentials.CredentialDetails.SAPDBName.ToString(),
                                Table = Credentials.CredentialDetails.Module,
                                Module = new_GroupKey
                            });
                            _context.SaveChanges();
                            errorRows.Add(new SyncResultRow(group.Key, "Failed", DateTime.Now, filePath, "ReferenceId number is missing."));
                            continue;
                        }
                        //Check Module and EntityType
                        if (Credentials.CredentialDetails.Module.Equals("ORIN"))
                        {   
                            string check_ARCM = $@"select top 1 NUMATCARD from dbo.ORIN where NUMATCARD  = '{group.Key}' and CANCELED = 'N'";

                            var cm_result = DataAccess.Select(connectionString, check_ARCM);

                            bool _cmExist = cm_result != null && cm_result.Rows.Count > 0;

                            if (_cmExist)
                            {
                                _context.SystemLogs.Add(new DomainLayer.Models.SystemLog
                                {
                                    Task = "Excel to SAP",
                                    CreateDate = DateTime.Now,
                                    ErrorMsg = $"This RTV Number already exists in SAP, skipping upload.",
                                    Database = Credentials.CredentialDetails.SAPDBName.ToString(),
                                    Module = new_GroupKey,
                                    Table = Credentials.CredentialDetails.Module
                                });
                                _context.SaveChanges();
                                errorRows.Add(new SyncResultRow(group.Key, "Failed", DateTime.Now, filePath, "This RTV Number already exists in SAP, skipping upload."));
                                continue;
                            }
                        }

                        var headerObj = new JObject();
                        var firstRow = group.First();

                        //for loop for the header fields
                        var headerFields = _context.Headers
                        .Where(x => x.MapId == mapId && x.DestinationTableName == "Header")
                        .ToList();


                        var unresolvedPaths = new List<string>(); // collects any failed/no-match lookups for this group
                        var headerContext = BuildRowContext(firstRow);
                        foreach (var header in headerFields)
                        {
                            string SourceField = header.SourceFieldId.ToString();
                            string DestinationFieldId = header.DestinationField.ToString();

                            if (DestinationFieldId.Equals("-"))
                            {
                                continue;
                            }

                            string ConditionalValue = header.ConditionalQuery.ToString();

                            string mapValue = string.Empty;
                     

                            switch (SourceField)
                            {
                                case "Query":
                                    // TODO: implement DB lookup / expression evaluation logic
                                    // once the query format/spec is defined (what does ConditionalValue reference?)
                                    //mapValue = EvaluateHeaderQuery(ConditionalValue, firstRow, mapId);
                                    mapValue = ResolveQueryValue(headerContext, ConditionalValue, Credentials, unresolvedPaths)?.ToString();
                                    break;

                                case "Static Value":
                                    // value is literal, taken from ConditionalQuery as-is
                                    mapValue = ConditionalValue;
                                    break;

                                default:
                                    // value comes from the Excel/CSV row itself, SourceFieldId is a column letter
                                    int colIndex = ColumnLetterToIndex(SourceField);
                                    mapValue = colIndex >= 0 && colIndex < firstRow.Count
                                        ? firstRow[colIndex]?.ToString()
                                        : null;

                                    mapValue = FormatDateIfApplicable(mapValue);

                                    break;
                            }

                            if (!string.IsNullOrWhiteSpace(DestinationFieldId))
                            {
                                headerObj[DestinationFieldId] = mapValue;
                            }
                          
                        }//end of Header
                        headerObj["U_Template_Name"] = Path.GetFileName(filePath);

                        var linesArray = new JArray();
                        //I need to loop all of the Lines
                        var DestinationTableName = _context.Headers
                         .Where(x => x.DestinationTableName != "Header")
                         .Select(x => x.DestinationTableName)
                         .Distinct()
                         .ToList();

                        foreach (var DestTable in DestinationTableName)
                        {
                            var lineFields = _context.Headers
                                .Where(x => x.MapId == mapId && x.DestinationTableName == DestTable)
                                .ToList();

                            var tableArray = new JArray();

                            foreach (var row in group) // per item on group
                            {
                                var lineObj = new JObject();
                                var lineContext = BuildRowContext(row);
                                foreach (var lineField in lineFields)
                                {
                                    string SourceField = lineField.SourceFieldId?.ToString();
                                    string DestinationFieldId = lineField.DestinationField?.ToString();
                                    if (DestinationFieldId.Equals("-"))
                                    {
                                        continue;
                                    }
                                    string ConditionalValue = lineField.ConditionalQuery?.ToString();
                                    string mapValue;

                                    switch (SourceField)
                                    {
                                        case "Query":
                                            // TODO: implement DB lookup / expression evaluation logic
                                            //mapValue = EvaluateHeaderQuery(ConditionalValue, row, mapId);
                                            // mapValue = ResolveQueryValue(headerContext, ConditionalValue, Credentials, unresolvedPaths)?.ToString();
                                            mapValue = ResolveQueryValue(lineContext, ConditionalValue, Credentials, unresolvedPaths, headerContext)?.ToString();
                                            break;

                                        case "Static Value":
                                            mapValue = ConditionalValue;
                                            break;

                                        default:
                                            int colIndex = ColumnLetterToIndex(SourceField);
                                            mapValue = colIndex >= 0 && colIndex < row.Count
                                                ? row[colIndex]?.ToString()
                                                : null;

                                            mapValue = FormatDateIfApplicable(mapValue);
                                            break;
                                    }

                                    if (!string.IsNullOrWhiteSpace(DestinationFieldId))
                                    {
                                        lineObj[DestinationFieldId] = mapValue;
                                    }
                                }

                                tableArray.Add(lineObj);
                            }

                            headerObj[DestTable] = tableArray;
                        }

                        payload.Add(headerObj);


                            string jsonPayload = headerObj.ToString(Newtonsoft.Json.Formatting.Indented);

                            var posting_result = await phelper.PostSAPAsync("POST", Credentials.CredentialDetails.EntityName, jsonPayload, "", Credentials, "", "", false, "");
                   
                        var json = JObject.Parse(posting_result);

                        if (json["error"] != null)
                        {
                            string value = posting_result;
                            if (PostingHelpers.IsValidJson(posting_result))
                            {
                                JObject jObject = JObject.Parse(posting_result);
                                value = jObject["error"]?["message"]?["value"]?.ToString() ?? posting_result.ToString();
                            }

                            //var checkMod = $"'{primaryKeyValue}'";
                            //Check if the SystemLog entry exists based on Module and ErrorMsg


                            var existingLog = _context.SystemLogs.FirstOrDefault(log =>
                                log.Module == new_GroupKey &&
                                log.ErrorMsg == value);

                            if (existingLog == null)
                            {
                                _context.SystemLogs.Add(new DomainLayer.Models.SystemLog
                                {
                                    Task = $@"Excel to SAP",
                                    CreateDate = DateTime.Now
                                    ,
                                    ApiUrl = $@" POST : {Credentials.CredentialDetails.SAPSldServer}:{Credentials.CredentialDetails.SAPLicensePort}/b1s/v1/{Credentials.CredentialDetails.EntityName}"
                                    ,
                                    Json = jsonPayload.ToString()
                                    ,
                                    ErrorMsg = value,//result.ToString(),
                                    Database = Credentials.CredentialDetails.SAPDBName.ToString(),
                                    Module = new_GroupKey
                                    ,
                                    Table = $@"{Credentials.CredentialDetails.Module}"
                                });
                                _context.SaveChanges();
                            }
                            // error branch
                            errorRows.Add(new SyncResultRow(group.Key, "Failed", DateTime.Now, filePath,value));
                        }
                        else
                        {
                            _context.PlatformTable.Add(new DomainLayer.Models.PlatformTable
                            {
                                ReferenceId = group.Key,
                                Platform = "SAP",
                                BuildJson = jsonPayload,
                                PlatformJson = posting_result,
                                SyncStatus = "Sync",
                                Fulfillment = Credentials.CredentialDetails.EntityName,
                                CreatedDate = DateTime.Now,
                                UploadDate = DateTime.Now,
                                Message = "Successfully Posted",
                            });
                            _context.SaveChanges();

                           
                            // success branch
                            successRows.Add(new SyncResultRow(group.Key, "Success", DateTime.Now, filePath, "Successfully Posted"));
                        }

                    }//end of groupFile

                    string baseName = Path.GetFileNameWithoutExtension(filePath);
                    string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");

                    if (successRows.Any())
                    {
                        string successFilePath = Path.Combine(donePath, $"{baseName}_Success_{timestamp}.xlsx");
                        WriteResultsToExcel(successFilePath, successRows);
                    }

                    if (errorRows.Any() && !string.IsNullOrWhiteSpace(errorPath))
                    {
                        string errorFilePath = Path.Combine(errorPath, $"{baseName}_Error_{timestamp}.xlsx");
                        WriteResultsToExcel(errorFilePath, errorRows);
                    }

                    // ---- move original file to backup once all groups for this file are done ----
                    try
                    {
                        string backupFilePath = Path.Combine(backupPath, Path.GetFileName(filePath));
                        if (File.Exists(backupFilePath))
                            backupFilePath = Path.Combine(backupPath, $"{baseName}_{timestamp}{Path.GetExtension(filePath)}");

                        File.Move(filePath, backupFilePath);
                    }
                    catch (Exception ex)
                    {
                        throw new Exception ($"Failed to move '{filePath}' to backup folder: {ex.Message}");
                    }


                }//end of for loop files




            } //end of MapIds


            var checkSystemLogs = _context.SystemLogs
                                   .Any(x => x.Table.Contains("Email Sent"));
            if (!checkSystemLogs)
            {
                await emailsent.SendEmail("NSEmailSender");
            }

                return "SAP Posting completed successfully";
        }

        public class SyncResultRow
        {
            public string ReferenceId { get; set; }
            public string Status { get; set; }
            public DateTime ProcessedDate { get; set; }
            public string FilePath { get; set; }
            public string ErrorMessage { get; set; }

            public SyncResultRow(string referenceId, string status, DateTime processedDate, string filePath, string errorMessage = null)
            {
                ReferenceId = referenceId;
                Status = status;
                ProcessedDate = processedDate;
                FilePath = filePath;
                ErrorMessage = errorMessage;
            }
        }

        private string FormatDateIfApplicable(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return value;

            string[] knownFormats = { "MM/dd/yyyy", "M/d/yyyy", "yyyy-MM-dd" };

            if (DateTime.TryParseExact(value, knownFormats, System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None, out DateTime parsedDate))
            {
                return parsedDate.ToString("yyyyMMdd");
            }

            // not a recognized date format — return as-is
            return value;
        }
        private object ResolveQueryValue(JToken context, string queryTemplate, PostingViewModel creds, List<string> unresolvedPaths, JToken fallbackContext = null)
        {
            if (string.IsNullOrWhiteSpace(queryTemplate))
                return null;

            string input = queryTemplate;

            var columnNames = Regex.Matches(queryTemplate, "#(.+?)#")
                .Cast<Match>()
                .Select(m => m.Groups[1].Value)
                .Distinct();

            foreach (var columnName in columnNames)
            {
                // try the immediate context first (line item), then fall back (order/header)
                var resolvedToken = context?.SelectToken(columnName) ?? fallbackContext?.SelectToken(columnName);

                if (resolvedToken == null || resolvedToken.Type == JTokenType.Null)
                {
                    unresolvedPaths?.Add($"Query token '#{columnName}#' unresolved in either item or header context (template: {queryTemplate})");
                    return null;
                }


                string safeValue = resolvedToken.ToString().Replace("'", "''");
                input = QueryAccess.ReplaceQueryParameter(input, $"#{columnName}#", safeValue.ToString(), "");
            }

            var connectionString = DataAccess.GetConnectionString(
                creds.CredentialDetails.SAPIPAddress,
                creds.CredentialDetails.SAPDBName,
                creds.CredentialDetails.SAPDBuser,
                creds.CredentialDetails.SAPDBPassword);

            var dtResult = DataAccess.Select(connectionString, input);

            if (dtResult == null || dtResult.Rows.Count == 0)
            {
                // legitimate "no match" case — e.g. CardCode not found in SAP — not an exception,
                // but the caller needs to know this field came back empty.
                unresolvedPaths?.Add($"Query returned no rows (template: {queryTemplate})");
                return null;
            }

            var rawValue = dtResult.Rows[0][0]?.ToString();
            if (string.IsNullOrEmpty(rawValue))
                return null;

            return rawValue;
        }
        private JObject BuildRowContext(List<object> row)
        {
            var obj = new JObject();
            for (int i = 0; i < row.Count; i++)
            {
                string colLetter = XLHelper.GetColumnLetterFromNumber(i + 1); // 1-based
                obj[colLetter] = row[i]?.ToString();
            }
            return obj;
        }
        private void WriteResultsToExcel(string filePath, List<SyncResultRow> rows)
        {
            if (rows == null || rows.Count == 0) return;

            try
            {
                using (var workbook = new ClosedXML.Excel.XLWorkbook())
                {
                    var worksheet = workbook.Worksheets.Add("Results");

                    string[] headers = { "ReferenceId", "Status", "ErrorMessage", "ProcessedDate", "TemplateName" };
                    for (int c = 0; c < headers.Length; c++)
                        worksheet.Cell(1, c + 1).Value = headers[c];

                    for (int r = 0; r < rows.Count; r++)
                    {
                        var row = rows[r];
                        worksheet.Cell(r + 2, 1).Value = row.ReferenceId;
                        worksheet.Cell(r + 2, 2).Value = row.Status;
                        worksheet.Cell(r + 2, 3).Value = row.ErrorMessage ?? string.Empty;
                        worksheet.Cell(r + 2, 4).Value = row.ProcessedDate;
                        worksheet.Cell(r + 2, 4).Style.DateFormat.Format = "yyyy-MM-dd HH:mm:ss";
                        worksheet.Cell(r + 2, 5).Value = row.FilePath;
                    }

                    worksheet.Columns().AdjustToContents();
                    workbook.SaveAs(filePath);
                }
            }
            catch (IOException)
            {
                // file locked / path invalid — rethrown for now; add logging here if you want a diagnostic trail
                throw;
            }
        }

        //private async Task<List<IGrouping<string, List<object>>>> ReadXlsxGroupedByColumn(string FileDirectory, string columnLetter, int startRow = 1)
        //{
        //    int colIndex = ColumnLetterToIndex(columnLetter);

        //    using (var workbook = new XLWorkbook(FileDirectory))
        //    {
        //        var ws = workbook.Worksheet(1);
        //        var lastRow = ws.LastRowUsed();
        //        var lastCol = ws.LastColumnUsed();
        //        if (lastRow == null || lastCol == null)
        //            return null;

        //        int rowCount = lastRow.RowNumber();
        //        int colCount = lastCol.ColumnNumber();

        //        if (colIndex < 0 || colIndex >= colCount)
        //            throw new ArgumentOutOfRangeException(nameof(columnLetter), $"Column '{columnLetter}' is outside the range of columns in the file (file has {colCount} columns).");

        //        if (startRow < 1 || startRow > rowCount)
        //            throw new ArgumentOutOfRangeException(nameof(startRow), $"Start row '{startRow}' is outside the range of rows in the file (file has {rowCount} rows).");

        //        var rows = new List<List<object>>();
        //        for (int row = startRow; row <= rowCount; row++)
        //        {
        //            var rowValues = new List<object>();
        //            for (int col = 1; col <= colCount; col++)
        //            {
        //                rowValues.Add(ws.Cell(row, col).Value);
        //            }
        //            rows.Add(rowValues);
        //        }

        //        return rows
        //            .GroupBy(r => colIndex < r.Count ? r[colIndex]?.ToString() : null)
        //            .ToList();
        //    }
        //}
        private async Task<List<IGrouping<string, List<object>>>> ReadXlsxGroupedByColumn(
    string FileDirectory, string columnLetter, string Module, string MapName, int startRow = 1)
        {
            int colIndex = ColumnLetterToIndex(columnLetter);

            using (var workbook = new XLWorkbook(FileDirectory))
            {
                var ws = workbook.Worksheet(1);
                var lastRow = ws.LastRowUsed();
                var lastCol = ws.LastColumnUsed();
                if (lastRow == null || lastCol == null)
                    return null;

                int rowCount = lastRow.RowNumber();
                int colCount = lastCol.ColumnNumber();

                if (colIndex < 0 || colIndex >= colCount)
                    throw new ArgumentOutOfRangeException(nameof(columnLetter), $"Column '{columnLetter}' is outside the range of columns in the file (file has {colCount} columns).");

                if (startRow < 1 || startRow > rowCount)
                    throw new ArgumentOutOfRangeException(nameof(startRow), $"Start row '{startRow}' is outside the range of rows in the file (file has {rowCount} rows).");

                var rows = new List<List<object>>();
                for (int row = startRow; row <= rowCount; row++)
                {
                    var rowValues = new List<object>(colCount);
                    for (int col = 1; col <= colCount; col++)
                    {
                        rowValues.Add(ws.Cell(row, col).Value);
                    }

                    // Skip rows that are "used" (formatting, borders, etc.) but have no actual data.
                    // LastRowUsed() includes formatting-only rows, which otherwise produce a phantom
                    // empty-key group — same root cause as the trailing-comma issue in the CSV reader.
                    if (rowValues.All(v => string.IsNullOrWhiteSpace(v?.ToString())))
                        continue;

                    rows.Add(rowValues);
                }

                if (rows.Count == 0)
                    return null;

                var groups = rows
                    .GroupBy(r => colIndex < r.Count ? r[colIndex]?.ToString()?.Trim() : null)
                    .ToList();

                bool applyCap = string.Equals(Module, "ORDR", StringComparison.OrdinalIgnoreCase)
                    && !string.IsNullOrWhiteSpace(MapName)
                    && new[] { "sm", "waltermart", "robinson", "sm snack" }
                        .Any(k => MapName.IndexOf(k, StringComparison.OrdinalIgnoreCase) >= 0);

                if (!applyCap)      
                    return groups;

                var result = new List<IGrouping<string, List<object>>>();
                const int maxRows = 13;

                foreach (var group in groups)
                {
                    var groupRows = group.ToList();
                    if (groupRows.Count <= maxRows)
                    {
                        result.Add(group);
                        continue;
                    }

                    for (int offset = 0; offset < groupRows.Count; offset += maxRows)
                    {
                        var chunk = groupRows.Skip(offset).Take(maxRows).ToList();
                        result.Add(new SimpleGrouping<string, List<object>>(group.Key, chunk));
                    }
                }

                return result;
            }
        }

        //    private async Task<List<IGrouping<string, List<object>>>> ReadXlsxGroupedByColumn(
        //string FileDirectory, string columnLetter, string Module, string MapName, int startRow = 1)
        //    {
        //        int colIndex = ColumnLetterToIndex(columnLetter);

        //        using (var workbook = new XLWorkbook(FileDirectory))
        //        {
        //            var ws = workbook.Worksheet(1);
        //            var lastRow = ws.LastRowUsed();
        //            var lastCol = ws.LastColumnUsed();
        //            if (lastRow == null || lastCol == null)
        //                return null;

        //            int rowCount = lastRow.RowNumber();
        //            int colCount = lastCol.ColumnNumber();

        //            if (colIndex < 0 || colIndex >= colCount)
        //                throw new ArgumentOutOfRangeException(nameof(columnLetter), $"Column '{columnLetter}' is outside the range of columns in the file (file has {colCount} columns).");

        //            if (startRow < 1 || startRow > rowCount)
        //                throw new ArgumentOutOfRangeException(nameof(startRow), $"Start row '{startRow}' is outside the range of rows in the file (file has {rowCount} rows).");

        //            var rows = new List<List<object>>();
        //            for (int row = startRow; row <= rowCount; row++)
        //            {
        //                var rowValues = new List<object>();
        //                for (int col = 1; col <= colCount; col++)
        //                {
        //                    rowValues.Add(ws.Cell(row, col).Value);
        //                }
        //                rows.Add(rowValues);
        //            }

        //            var groups = rows
        //                .GroupBy(r => colIndex < r.Count ? r[colIndex]?.ToString() : null)
        //                .ToList();

        //            bool applyCap = string.Equals(Module, "ORDR", StringComparison.OrdinalIgnoreCase)
        //                && !string.IsNullOrWhiteSpace(MapName)
        //                && new[] { "sm", "waltermart", "robinson","sm snack"}
        //                    .Any(k => MapName.IndexOf(k, StringComparison.OrdinalIgnoreCase) >= 0);

        //            if (!applyCap)
        //                return groups;

        //            var result = new List<IGrouping<string, List<object>>>();
        //            const int maxRows = 13;

        //            foreach (var group in groups)
        //            {
        //                var groupRows = group.ToList();
        //                if (groupRows.Count <= maxRows)
        //                {
        //                    result.Add(group);
        //                    continue;
        //                }

        //                for (int offset = 0; offset < groupRows.Count; offset += maxRows)
        //                {
        //                    var chunk = groupRows.Skip(offset).Take(maxRows).ToList();
        //                    result.Add(new SimpleGrouping<string, List<object>>(group.Key, chunk));
        //                }
        //            }

        //            return result;
        //        }
        //    }
        //private async Task <List<IGrouping<string, List<object>>>> ReadCsvGroupedByColumn(string FileDirectory, string columnLetter,string Module,string MapName, int startRow = 1)
        //{
        //    int colIndex = ColumnLetterToIndex(columnLetter);

        //    if (!File.Exists(FileDirectory))
        //        return null;

        //    var lines = File.ReadAllLines(FileDirectory);
        //    if (lines.Length == 0)
        //        return null;

        //    if (startRow < 1 || startRow > lines.Length)
        //        throw new ArgumentOutOfRangeException(nameof(startRow), $"Start row '{startRow}' is outside the range of rows in the file (file has {lines.Length} rows).");

        //    var rows = new List<List<object>>();

        //    // startRow is 1-based, array is 0-based
        //    for (int i = startRow - 1; i < lines.Length; i++)
        //    {
        //        var line = lines[i];
        //        if (string.IsNullOrWhiteSpace(line))
        //            continue;

        //        var fields = ParseCsvLine(line);
        //        var rowValues = new List<object>(fields.Length);
        //        foreach (var field in fields)
        //            rowValues.Add(field);

        //        rows.Add(rowValues);
        //    }

        //    if (rows.Count == 0)
        //        return null;

        //    int colCount = rows.Max(r => r.Count);
        //    if (colIndex < 0 || colIndex >= colCount)
        //        throw new ArgumentOutOfRangeException(nameof(columnLetter), $"Column '{columnLetter}' is outside the range of columns in the file (file has {colCount} columns).");

        //    //Applied logic for splitting groups based on Module and MapName

        //    var groups = rows
        //.GroupBy(r => colIndex < r.Count ? r[colIndex]?.ToString() : null)
        //.ToList();

        //    bool applyCap = string.Equals(Module, "ORDR", StringComparison.OrdinalIgnoreCase)
        //        && !string.IsNullOrWhiteSpace(MapName)
        //        && new[] { "sm", "waltermart", "robinson","sm snack" }
        //            .Any(k => MapName.IndexOf(k, StringComparison.OrdinalIgnoreCase) >= 0);

        //    if (!applyCap)
        //        return groups;

        //    var result = new List<IGrouping<string, List<object>>>();
        //    const int maxRows = 13;

        //    foreach (var group in groups)
        //    {
        //        var groupRows = group.ToList();
        //        if (groupRows.Count <= maxRows)
        //        {
        //            result.Add(group);
        //            continue;
        //        }

        //        for (int offset = 0; offset < groupRows.Count; offset += maxRows)
        //        {
        //            var chunk = groupRows.Skip(offset).Take(maxRows).ToList();
        //            result.Add(new SimpleGrouping<string, List<object>>(group.Key, chunk));
        //        }
        //    }


        //    return result;

        //    //return rows
        //    //    .GroupBy(r => colIndex < r.Count ? r[colIndex]?.ToString() : null)
        //    //    .ToList();
        //}
        private async Task<List<IGrouping<string, List<object>>>> ReadCsvGroupedByColumn(string FileDirectory, string columnLetter, string Module, string MapName, int startRow = 1)
        {
            int colIndex = ColumnLetterToIndex(columnLetter);

            if (!File.Exists(FileDirectory))
                return null;

            var lines = File.ReadAllLines(FileDirectory);
            if (lines.Length == 0)
                return null;

            if (startRow < 1 || startRow > lines.Length)
                throw new ArgumentOutOfRangeException(nameof(startRow), $"Start row '{startRow}' is outside the range of rows in the file (file has {lines.Length} rows).");

            var rows = new List<List<object>>();

            // startRow is 1-based, array is 0-based
            for (int i = startRow - 1; i < lines.Length; i++)
            {
                var line = lines[i];
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                var fields = ParseCsvLine(line);

                // Skip rows that are structurally "blank" (e.g. ",,,,,,," from trailing
                // Excel/export rows) — IsNullOrWhiteSpace(line) above won't catch these
                // because the commas themselves aren't whitespace.
                if (fields.All(f => string.IsNullOrWhiteSpace(f)))
                    continue;

                var rowValues = new List<object>(fields.Length);
                foreach (var field in fields)
                    rowValues.Add(field);

                rows.Add(rowValues);
            }

            if (rows.Count == 0)
                return null;

            int colCount = rows.Max(r => r.Count);
            if (colIndex < 0 || colIndex >= colCount)
                throw new ArgumentOutOfRangeException(nameof(columnLetter), $"Column '{columnLetter}' is outside the range of columns in the file (file has {colCount} columns).");

            // Applied logic for splitting groups based on Module and MapName
            var groups = rows
                .GroupBy(r => colIndex < r.Count ? r[colIndex]?.ToString()?.Trim() : null)
                .ToList();

            bool applyCap = string.Equals(Module, "ORDR", StringComparison.OrdinalIgnoreCase)
                && !string.IsNullOrWhiteSpace(MapName)
                && new[] { "sm", "waltermart", "robinson", "sm snack" }
                    .Any(k => MapName.IndexOf(k, StringComparison.OrdinalIgnoreCase) >= 0);

            if (!applyCap)
                return groups;

            var result = new List<IGrouping<string, List<object>>>();
            const int maxRows = 13;

            foreach (var group in groups)
            {
                var groupRows = group.ToList();
                if (groupRows.Count <= maxRows)
                {
                    result.Add(group);
                    continue;
                }

                for (int offset = 0; offset < groupRows.Count; offset += maxRows)
                {
                    var chunk = groupRows.Skip(offset).Take(maxRows).ToList();
                    result.Add(new SimpleGrouping<string, List<object>>(group.Key, chunk));
                }
            }

            return result;
        }
        private sealed class SimpleGrouping<TKey, TElement> : IGrouping<TKey, TElement>
        {
            private readonly List<TElement> _items;
            public TKey Key { get; }

            public SimpleGrouping(TKey key, List<TElement> items)
            {
                Key = key;
                _items = items;
            }

            public IEnumerator<TElement> GetEnumerator() => _items.GetEnumerator();
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        private int ColumnLetterToIndex(string columnLetter)
        {
            if (string.IsNullOrWhiteSpace(columnLetter))
                throw new ArgumentException("Column letter cannot be null or empty.", nameof(columnLetter));

            // XLHelper.GetColumnNumberFromLetter returns a 1-based column number
            int oneBasedIndex = XLHelper.GetColumnNumberFromLetter(columnLetter.Trim().ToUpperInvariant());
            return oneBasedIndex - 1;
        }
        private string[] ParseCsvLine(string line)
        {
            var fields = new List<string>();
            var current = new StringBuilder();
            bool inQuotes = false;

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];

                if (inQuotes)
                {
                    if (c == '"')
                    {
                        if (i + 1 < line.Length && line[i + 1] == '"')
                        {
                            current.Append('"');
                            i++; // skip escaped quote
                        }
                        else
                        {
                            inQuotes = false;
                        }
                    }
                    else
                    {
                        current.Append(c);
                    }
                }
                else
                {
                    if (c == '"')
                    {
                        inQuotes = true;
                    }
                    else if (c == ',')
                    {
                        fields.Add(current.ToString());
                        current.Clear();
                    }
                    else
                    {
                        current.Append(c);
                    }
                }
            }

            fields.Add(current.ToString());
            return fields.ToArray();
        }

    }
}
