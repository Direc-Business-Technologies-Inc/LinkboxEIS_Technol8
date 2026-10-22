
using DataAccessLayer.Class;
using DomainLayer;
using DomainLayer.Models;
using DomainLayer.ViewModels;
using LinkBoxUI.Context;
using LinkBoxUI.Helpers;
using Newtonsoft.Json.Linq;
using Remotion.Data.Linq.Clauses;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static LinkBoxUI.Services.PrimaryAccounts.PrimaryAcountServices;

namespace LinkBoxUI.Services.BIR_EIS
{
    public class EISServices
    {
        private readonly LinkboxDb _context = new LinkboxDb();

        private const string QuerySource = "Query";
        private const string StaticSource = "Static Value";

        // JSON arrays in the target schema. Add more here if the schema grows.
        private static readonly HashSet<string> ArrayRoots =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "ItemList" };

        private static readonly Regex LeadingTop1 =
            new Regex(@"^\s*SELECT\s+TOP\s*\(?\s*1\s*\)?\s+", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private class ArrayPath
        {
            public string Root { get; set; }
            public string Property { get; set; }
        }
        public PostingViewModel OPSGetFileToSAPCredentialsMapCode(int mapid)
        {
            var credential = (
                from fm in _context.FieldMappings
                where fm.MapId == mapid

                join p in _context.PathSetup
                    on fm.PathCode equals p.PathId.ToString() into pj
                from ps in pj.DefaultIfEmpty()

                join q in _context.QueryManager
                    on fm.QueryCode equals q.QueryCode into qj
                from qm in qj.DefaultIfEmpty()

                join s in _context.SAPSetup
                    on fm.SAPCode equals s.SAPCode into sj
                from sap in sj.DefaultIfEmpty()

                join m in _context.ModuleSetup
                    on fm.ModuleName equals m.ModuleCode into mj
                from ms in mj.DefaultIfEmpty()

                orderby fm.MapId
                select new PostingViewModel.Credential
                {
                    // FieldMapping
                    MapId = fm.MapId,
                    MapName = fm.MapName ?? "",
                    Module = fm.ModuleName ?? "",
                    FileType = fm.FileType ?? "",
                    FileName = fm.FileName ?? "",

                    // PathSetup
                    LocalPath = ps.LocalPath ?? "",
                    FTPPath = ps.RemotePath ?? "",
                    BackupPath = ps.BackupPath ?? "",
                    ErrorPath = ps.ErrorPath ?? "",

                    // QueryManager
                    QueryString = qm.QueryString ?? "",

                    // SAPSetup
                    SAPDBName = sap.SAPDBName ?? "",
                    SAPIPAddress = sap.SAPIPAddress ?? "",
                    SAPSldServer = sap.SAPSldServer ?? "",
                    SAPServerName = sap.SAPServerName ?? "",
                    SAPLicensePort = sap.SAPLicensePort,   // see note 1
                    SAPDBuser = sap.SAPDBuser ?? "",
                    SAPDBPassword = sap.SAPDBPassword ?? "",
                    SAPUser = sap.SAPUser ?? "",
                    SAPPassword = sap.SAPPassword ?? "",

                    // ModuleSetup
                    EntityType = ms.EntityType ?? "",
                    EntityName = ms.EntityName ?? "",
                    PrimaryKey = ms.PrimaryKey ?? ""
                }).FirstOrDefault();

            return new PostingViewModel
            {
                CredentialDetails = credential
            };
        }
        //private const string HeaderTable = "Header";
        //private const string QuerySource = "Query";
        //private const string StaticSource = "Static Value";

    //    private static readonly Regex LeadingTop1 =
    //new Regex(@"^\s*SELECT\s+TOP\s*\(?\s*1\s*\)?\s+", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        public async Task<string> GenerateInvoiceJson(string code, int[] mapIds)
        {
            var sched = _context.Schedules.FirstOrDefault(s => s.SchedCode == code);
            if (sched == null)
                throw new InvalidOperationException($"Schedule '{code}' not found.");

            sched.IsRunning = true;
            sched.RunTime = DateTime.Now;
            _context.SaveChanges();

            try
            {
                foreach (var mapId in mapIds)
                {
                    var creds = OPSGetFileToSAPCredentialsMapCode(mapId).CredentialDetails;
                    if (creds == null)
                    {
                        Console.WriteLine($"No credentials found for MapId {mapId}, skipping.");
                        continue;
                    }

                    var localPath = creds.LocalPath;
                    if (string.IsNullOrWhiteSpace(localPath) || !Directory.Exists(localPath))
                    {
                        Console.WriteLine($"LocalPath '{localPath}' is missing or invalid for MapId {mapId}, skipping.");
                        continue;
                    }

                    if (string.IsNullOrWhiteSpace(creds.QueryString))
                    {
                        Console.WriteLine($"No query configured for MapId {mapId}, skipping.");
                        continue;
                    }

                    // Strip a leading "SELECT TOP 1" (the stored query is a single-row sample)
                    var queryString = LeadingTop1.Replace(creds.QueryString, "SELECT ", 1);

                    var connectionString = DataAccess.GetConnectionString(
                        creds.SAPIPAddress, creds.SAPDBName, creds.SAPDBuser, creds.SAPDBPassword);

                    DataTable dtResult = DataAccess.Select(connectionString, queryString);
                    if (dtResult.Rows.Count == 0)
                    {
                        Console.WriteLine($"Query returned no rows for MapId {mapId}.");
                        continue;
                    }

                    // Load the mapping once per map, not once per document
                    var allFields = _context.Headers.AsNoTracking()
                        .Where(x => x.MapId == mapId)
                        .ToList();

                    var keyColumn = allFields
                        .Where(x => x.IsKeyValue)
                        .Select(x => x.SourceFieldId?.ToString())
                        .FirstOrDefault();

                    if (string.IsNullOrWhiteSpace(keyColumn) || !dtResult.Columns.Contains(keyColumn))
                    {
                        Console.WriteLine($"Key column '{keyColumn}' is not configured or not in the query result for MapId {mapId}, skipping.");
                        continue;
                    }

                    var activeFields = allFields
                        .Where(x => !string.IsNullOrWhiteSpace(x.DestinationField) && x.DestinationField != "-")
                        .ToList();

                    var arrayRoots = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "ItemList" };

                    var parsed = activeFields.Select(f =>
                    {
                        var dest = f.DestinationField;
                        var dot = dest.IndexOf('.');
                        var root = dot > 0 ? dest.Substring(0, dot).Replace("[]", "") : null;
                        var isLine = root != null && arrayRoots.Contains(root);

                        return new
                        {
                            Field = f,
                            Root = isLine ? root : null,
                            Property = isLine ? dest.Substring(dot + 1) : null
                        };
                    }).ToList();

                    var headerFields = parsed.Where(p => p.Root == null).Select(p => p.Field).ToList();

                    var lineGroups = parsed.Where(p => p.Root != null).GroupBy(p => p.Root).ToList();

                    var queryCache = new Dictionary<string, JToken>();   // one DB hit per Query field per map
                    var successRows = new List<SyncResultRow>();
                    var errorRows = new List<SyncResultRow>();

                    var grouped = dtResult.AsEnumerable()
                        .GroupBy(r => Convert.ToString(r[keyColumn]));

                    foreach (var group in grouped)
                    {
                        var rows = group.ToList();
                        var groupLabel = $"[{creds.MapName}] - {group.Key}";
                        Console.WriteLine($"{groupLabel} ({rows.Count} rows)");

                        try
                        {
                            var headerObj = new JObject();
                            var firstRow = rows[0];

                            foreach (var field in headerFields)
                            {
                                var value = ResolveValue(field.SourceFieldId?.ToString(), field.ConditionalQuery?.ToString(),
                                                         firstRow, field.DestinationField, queryCache, connectionString);
                                SetPath(headerObj, field.DestinationField, value);
                            }

                            foreach (var lineGroup in lineGroups)
                            {
                                var tableArray = new JArray();
                                foreach (var row in rows)
                                {
                                    var lineObj = new JObject();
                                    foreach (var item in lineGroup)
                                    {
                                        var field = item.Field;
                                        var value = ResolveValue(field.SourceFieldId?.ToString(), field.ConditionalQuery?.ToString(),
                                                                 row, field.DestinationField, queryCache, connectionString);
                                        SetPath(lineObj, item.Property, value);
                                    }
                                    tableArray.Add(lineObj);
                                }
                                headerObj[lineGroup.Key] = tableArray;
                            }

                                var payload = new JArray { headerObj };
                            string jsonPayload = headerObj.ToString(Newtonsoft.Json.Formatting.Indented);

                            var savedPath = SaveJsonFile(localPath, creds.MapName, group.Key, jsonPayload);
                            // TODO: post jsonPayload / write the file, then add to successRows
                        }
                        catch (Exception ex)
                        {
                            // One bad document should not stop the rest of the batch
                            Console.WriteLine($"{groupLabel} failed: {ex.Message}");
                            // TODO: add to errorRows
                        }
                    }
                }
            }
            finally
            {
                sched.IsRunning = false;
                _context.SaveChanges();
            }

            return "Success";
        }

        private static JToken ResolveValue(
            string source, string conditional, DataRow row, string destination,
            Dictionary<string, JToken> queryCache, string connectionString)
        {
            switch (source)
            {
                case StaticSource:
                    return new JValue(conditional ?? "");

                case QuerySource:
                    if (string.IsNullOrWhiteSpace(conditional))
                        throw new InvalidOperationException($"Query field for '{destination}' has no query text.");

                    if (!queryCache.TryGetValue(conditional, out var cached))
                    {
                        var dt = DataAccess.Select(connectionString, conditional);
                        cached = dt.Rows.Count > 0 ? ToToken(dt.Rows[0][0]) : JValue.CreateNull();
                        queryCache[conditional] = cached;
                    }
                    return cached.DeepClone();

                default:
                    if (string.IsNullOrWhiteSpace(source) || !row.Table.Columns.Contains(source))
                        throw new InvalidOperationException($"Source column '{source}' (-> {destination}) is not in the query result.");
                    return ToToken(row[source]);
            }
        }

        private static JToken ToToken(object value) =>
            value == null || value == DBNull.Value ? JValue.CreateNull() : JToken.FromObject(value);

       
        private static void SetPath(JObject root, string path, JToken value)
        {
            var parts = path.Split('.');
            var current = root;
            for (int i = 0; i < parts.Length - 1; i++)
            {
                if (!(current[parts[i]] is JObject next))
                {
                    next = new JObject();
                    current[parts[i]] = next;
                }
                current = next;
            }
            current[parts[parts.Length - 1]] = value;
        }

        private static string SaveJsonFile(string localPath, string mapName, string key, string json)
        {
            string Sanitize(string s)
            {
                foreach (var c in Path.GetInvalidFileNameChars())
                    s = s.Replace(c, '_');
                return s.Trim();
            }

            var fileName = $"{Sanitize(mapName)}_{Sanitize(key)}_{DateTime.Now:yyyyMMddHHmmssfff}.json";
            var finalPath = Path.Combine(localPath, fileName);
            var tempPath = finalPath + ".tmp";

            // UTF-8 without BOM: many APIs and parsers choke on the BOM
            File.WriteAllText(tempPath, json, new UTF8Encoding(false));
            File.Move(tempPath, finalPath);   // atomic rename, so readers never see a half-written file

            return finalPath;
        }
    }

}