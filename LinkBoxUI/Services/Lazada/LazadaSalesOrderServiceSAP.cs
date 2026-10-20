
using DataAccessLayer.Class;
using DomainLayer;
using DomainLayer.Models;
using DomainLayer.ViewModels;
using LinkBoxUI.Context;
using LinkBoxUI.Helpers;
using LinkBoxUI.Helpers.Platforms;
using LinkBoxUI.Helpers.Tiktok;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static LinkBoxUI.Services.Tiktok.TikTokSalesOrderServiceSAP;

namespace LinkBoxUI.Services.Lazada
{
    public class LazadaSalesOrderServiceSAP
    {
        private readonly LinkboxDb _context = new LinkboxDb();
        public PostHelper phelper = new PostHelper();
        EmailSenderServiceHelper emailsent = new EmailSenderServiceHelper();
        public PostingViewModel OPSGetFileToSAPCredentialsMapCode(int mapid)
        {
            PostingViewModel model = new PostingViewModel();
            model.CredentialDetails = _context.FieldMappings.Where(x => x.MapId == mapid)

                        .GroupJoin(_context.SAPSetup
                                    , fm => fm.SAPCode
                                    , so => so.SAPCode
                                    , (fm, so) => so.Select(sel => new { fm = fm, so = sel }).DefaultIfEmpty(new { fm = fm, so = (SAPSetup)null }))
                        .SelectMany(g1 => g1)
                        .GroupJoin(_context.ModuleSetup
                                    , fmstd => fmstd.fm.ModuleName
                                    , msd => msd.ModuleCode
                                    , (fmstd, msd) => msd.Select(sel => new { fmstd = fmstd, msd = sel }).DefaultIfEmpty(new { fmstd = fmstd, msd = (ModuleSetup)null }))
                        .SelectMany(g2 => g2).ToList()
                        .Select(f => new PostingViewModel.Credential
                        {
                            MapId = f.fmstd.fm.MapId,
                            Module = f.fmstd.fm.ModuleName,

                            SAPDBName = f.fmstd.so.SAPDBName,
                            SAPIPAddress = f.fmstd.so.SAPIPAddress,
                            SAPSldServer = f.fmstd.so.SAPSldServer,
                            SAPServerName = f.fmstd.so.SAPServerName,
                            SAPLicensePort = f.fmstd.so.SAPLicensePort,
                            SAPDBuser = f.fmstd.so.SAPDBuser,
                            SAPDBPassword = f.fmstd.so.SAPDBPassword,
                            SAPUser = f.fmstd.so.SAPUser,
                            SAPPassword = f.fmstd.so.SAPPassword,

                            EntityType = f?.msd?.EntityType ?? "",
                            EntityName = f?.msd?.EntityName ?? "",
                            PrimaryKey = f?.msd.PrimaryKey ?? "",
                        }).FirstOrDefault();
            return model;
        }
        public async Task<string> LazadaOrderPostToSAP(int[] MapIds, string Code, string DateFrom = null, string DateTo = null)
        {
            var schedule = _context.Schedules.FirstOrDefault(x => x.SchedCode == Code);
            if (schedule == null)
                throw new InvalidOperationException($"No schedule found for code '{Code}'.");

            foreach (var mapId in MapIds)
            {
                //var apiCode = await _context.FieldMappings
                //   .Where(x => x.MapId == mapId)
                //   .Select(x => x.APICode)
                //   .FirstOrDefaultAsync();

                var mapping = await _context.FieldMappings
                .Where(x => x.MapId == mapId)
                .Select(x => new
                {
                    x.APICode,
                    x.MapName,
                    
                })
                .FirstOrDefaultAsync();

                var apiCode = mapping?.APICode;
                var mapName = mapping?.MapName;

                if (string.IsNullOrEmpty(apiCode))
                    continue;

                var apiSetup = await _context.APISetups
                    .FirstOrDefaultAsync(x => x.APICode == apiCode && x.IsActive);

                if (apiSetup == null)
                    continue;


                var orders = await LazadaHelper.GetLazadaOrdersAsync(apiSetup, DateFrom, DateTo);

                if (orders == null || !orders.Any())
                    return "No Orders Fetch";

                var builtPayloads = new List<JObject>();


                var Creds = OPSGetFileToSAPCredentialsMapCode(mapId);

                var headerFields = _context.Headers
                    .Where(x => x.MapId == mapId && x.DestinationTableName == "Header")
                    .ToList();

                var lineFields = _context.Headers
                    .Where(x => x.MapId == mapId && x.DestinationTableName != "Header")
                    .ToList();

                if (!headerFields.Any())
                    continue;

                var lineFieldGroups = lineFields.GroupBy(x => x.DestinationTableName).ToList();

                foreach (var order in orders)
                {
                    schedule.IsRunning = true;
                    schedule.RunTime = DateTime.Now;
                    _context.SaveChanges();

                    var OrderNumber = order.order_id.ToString();
                    var packageId = order.items?.FirstOrDefault()?.package_id ?? order.package_id ?? "";
                    if (string.IsNullOrEmpty(OrderNumber))
                        continue;

                    bool exist = _context.PlatformTable.Any(x => x.ReferenceId == OrderNumber && x.Platform == "Lazada" && x.SyncStatus == "Sync" && x.OrderNumber == packageId);
                    if (exist) continue;

                    var sapPostResult = await CreateSAPJson(order, headerFields, lineFieldGroups, Creds, mapId);

                    var result = sapPostResult.PostingResult;

                    if (sapPostResult.PostingResult == null)
                    {
                        // blocked — missing required field, nothing was posted
                        continue;
                    }

                    var sapJson = sapPostResult.JsonPayload;
                    var json = JObject.Parse(result);

                    if (json["error"] != null)
                    {
                        string value = result;
                        if (PostingHelpers.IsValidJson(result))
                        {
                            JObject jObject = JObject.Parse(result);
                            value = jObject["error"]?["message"]?["value"]?.ToString() ?? result.ToString();
                        }

                        //var checkMod = $"'{primaryKeyValue}'";
                        //Check if the SystemLog entry exists based on Module and ErrorMsg
                        var new_OrderNumber = $"[{mapName}] - {OrderNumber}";
                        

                        var existingLog = _context.SystemLogs.FirstOrDefault(log =>
                            log.Module == new_OrderNumber &&
                            log.ErrorMsg == value);

                        if (existingLog == null && !string.IsNullOrEmpty(OrderNumber))
                        {
                            _context.SystemLogs.Add(new DomainLayer.Models.SystemLog
                            {
                                Task = $@"LAZADA_SALES_ORDER",
                                CreateDate = DateTime.Now
                                ,
                                ApiUrl = $@" POST : {Creds.CredentialDetails.SAPSldServer}:{Creds.CredentialDetails.SAPLicensePort}/b1s/v1/Orders"
                                ,
                                Json = sapJson.ToString()
                                ,
                                ErrorMsg = value,//result.ToString(),
                                Database = Creds.CredentialDetails.SAPDBName.ToString(),
                                Module = new_OrderNumber
                                ,
                                Table = $@"{Creds.CredentialDetails.Module}"
                            });
                            _context.SaveChanges();
                        }

                    }
                    else
                    {
                        

                        _context.PlatformTable.Add(new DomainLayer.Models.PlatformTable
                        {
                            ReferenceId = OrderNumber,
                            Platform = "Lazada",
                            BuildJson = sapJson,
                            PlatformJson = result,
                            SyncStatus = "Sync",
                            Fulfillment = mapName,
                            OrderNumber = packageId,
                            CreatedDate = DateTime.Now,
                            UploadDate = DateTime.Now,
                            Message = "Successfully Posted",
                        });
                        _context.SaveChanges();
                    }
                }

            }

            //var checkSystemLogs = _context.SystemLogs
            //           .Any(x => x.Table.Contains("Email Sent"));
            //if (!checkSystemLogs)
            //{
            //    await emailsent.SendEmail("NSEmailSender");
            //}

            return "SAP Posting completed successfully";

        }
        private async Task<SapPostResult> CreateSAPJson(
             LazadaOrderModel order,
             List<Header> headerFields,
             List<IGrouping<string, Header>> lineFieldGroups,
             PostingViewModel Creds,
             int mapId)
        {
            var orderJson = JObject.FromObject(order);
            var sapPayload = new JObject();
            var unresolvedPaths = new List<string>();   // ← declare per order
            foreach (var field in headerFields)
            {
                var value = await ResolveValue(orderJson, field, field.ConditionalQuery, Creds, unresolvedPaths, null);
                sapPayload[field.DestinationField] = value != null ? JToken.FromObject(value) : null;
            }

            var poNo = sapPayload["U_PONo"]?.ToString();

            if (string.IsNullOrWhiteSpace(poNo))
            {
                return new SapPostResult
                {
                    PostingResult = null,   // see point 3 below on why not a string here
                    JsonPayload = sapPayload.ToString(Newtonsoft.Json.Formatting.Indented),
                };
            }

            foreach (var group in lineFieldGroups)
            {
                var arraySourcePaths = group
                    .Select(f => SplitArrayPath(f.SourceFieldId).ArrayPath)
                    .Where(p => p != null)
                    .Distinct()
                    .ToList();

                if (arraySourcePaths.Count > 1)
                    throw new InvalidOperationException(
                        $"MapId {mapId}, table '{group.Key}': fields map from multiple different source arrays ({string.Join(", ", arraySourcePaths)}). Each destination table must come from one source array.");

                var arrayPath = arraySourcePaths.FirstOrDefault();
                var sourceItems = arrayPath != null ? orderJson.SelectToken(arrayPath) as JArray : null;
                if (sourceItems == null)
                    continue;

                sourceItems = MergeLineItemsByProduct(sourceItems, "sku_id", "Quantity");

                var lineArray = new JArray();
                foreach (var item in sourceItems)
                {
                    var lineRow = new JObject();
                    foreach (var field in group)
                    {
                        object value;

                        if (field.SourceFieldId == "Query" || field.SourceFieldId == "Static Value")
                        {
                            value = await ResolveValue(item, field, field.ConditionalQuery, Creds, unresolvedPaths, orderJson);
                        }
                        else
                        {
                            var parts = SplitArrayPath(field.SourceFieldId);
                            var itemOnlyField = new Header
                            {
                                SourceFieldId = parts.ItemPath,
                                DestinationField = field.DestinationField,
                                ConditionalQuery = field.ConditionalQuery,
                                DataType = field.DataType
                            };
                            value = await ResolveValue(item, itemOnlyField, field.ConditionalQuery, Creds, unresolvedPaths);
                        }

                        lineRow[field.DestinationField] = value != null ? JToken.FromObject(value) : null;
                    }
                    lineRow["Quantity"] = item.SelectToken("Quantity") ?? 0;
                    lineArray.Add(lineRow);
                }
                sapPayload[group.Key] = lineArray;
            }

            var jsonToPost = sapPayload.ToString(Newtonsoft.Json.Formatting.Indented);
            var posting_result = await phelper.PostSAPAsync("POST", $@"Orders", jsonToPost, "", Creds, "", "", false, "");
            //return posting_result;
            return new SapPostResult
            {
                PostingResult = posting_result,
                JsonPayload = jsonToPost
            };
        }
        private JArray MergeLineItemsByProduct(JArray items, string groupKeyField, string computedQuantityField)
        {
            var merged = new JArray();

            var groups = items
                .GroupBy(item => item.SelectToken(groupKeyField)?.ToString() ?? Guid.NewGuid().ToString());
            // fallback to a unique key if product_id is missing, so we don't accidentally merge unrelated items

            foreach (var group in groups)
            {
                // use the first occurrence as the template row (keeps price, name, sku, etc.)
                var mergedItem = (JObject)group.First().DeepClone();

                // quantity = how many line items shared this same product_id
                int qty = group.Count();

                mergedItem[computedQuantityField] = qty;

                merged.Add(mergedItem);
            }

            return merged;
        }

        private async Task<object> ResolveValue(JToken context, Header field, string conditionalQuery, PostingViewModel Creds, List<string> unresolvedPaths, JToken fallbackContext = null)
        {
            try
            {
                switch (field.SourceFieldId)
                {
                    case "Static Value":
                        return field.ConditionalQuery;

                    case "Query":
                        return ResolveQueryValue(context, field.ConditionalQuery, Creds, unresolvedPaths, fallbackContext);
                    //return null;

                    default:
                        var token = context?.SelectToken(field.SourceFieldId);
                        if (token != null && token.Type != JTokenType.Null)
                            return token.ToObject<object>();

                        unresolvedPaths?.Add(field.SourceFieldId);
                        return null;
                }

            }
            catch (Exception ex)
            {
                throw new Exception(ex.ToString());
            }

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

                //input = QueryAccess.ReplaceQueryParameter(input, $"#{columnName}#", resolvedToken.ToString(), "");
                input = QueryAccess.ReplaceQueryParameter(input, $"#{columnName}#", resolvedToken.ToString().Replace("'", "''"), "");

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

            //if (int.TryParse(rawValue, out int intValue)) return intValue;
            //if (double.TryParse(rawValue, out double doubleValue)) return doubleValue;
            return rawValue;
        }
        private class SourcePathParts
        {
            public string ArrayPath { get; set; }
            public string ItemPath { get; set; }
        }

        private static SourcePathParts SplitArrayPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return new SourcePathParts { ArrayPath = null, ItemPath = path };

            var idx = path.IndexOf("[]", StringComparison.Ordinal);
            if (idx < 0)
                return new SourcePathParts { ArrayPath = null, ItemPath = path };

            return new SourcePathParts
            {
                ArrayPath = path.Substring(0, idx),
                ItemPath = path.Substring(idx + 2).TrimStart('.')
            };
        }
    }
}