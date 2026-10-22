using DataAccessLayer.Class;
using LinkBoxUI.Context;
using LinkBoxUI.Services.BIR_EIS;
using Microsoft.Graph.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.Entity;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Hosting;
using System.Web.Http;

namespace LinkBoxUI.Controllers.api
{
    [RoutePrefix("EIS")]
    public class EISController : ApiController
    {
        LinkboxDb _context = new LinkboxDb();
        EISServices eis = new EISServices();
        [HttpGet]
        [Route("api/GetSourceColumns")]
        public IHttpActionResult GetSourceColumns(string queryCode, string sapCode)
        {
            if (string.IsNullOrWhiteSpace(queryCode) || string.IsNullOrWhiteSpace(sapCode))
                return BadRequest("queryCode and sapCode are required.");

            var queryString = _context.QueryManager
                .AsNoTracking()
                .Where(x => x.QueryCode == queryCode)
                .Select(x => x.QueryString)
                .FirstOrDefault();

            if (string.IsNullOrWhiteSpace(queryString))
                return NotFound();

            var sap = _context.SAPSetup
                .AsNoTracking()
                .Where(x => x.SAPCode == sapCode)
                .Select(x => new { x.SAPIPAddress, x.SAPDBName, x.SAPDBuser, x.SAPDBPassword })
                .FirstOrDefault();

            if (sap == null)
                return NotFound();

            try
            {
                var connectionString = DataAccess.GetConnectionString(
                    sap.SAPIPAddress, sap.SAPDBName, sap.SAPDBuser, sap.SAPDBPassword);

                DataTable table = DataAccess.Select(connectionString, queryString);

                var columns = table.Columns
                    .Cast<DataColumn>()
                    .Select(c => c.ColumnName)
                    .ToArray();

                return Ok(columns);
            }
            catch (SqlException ex)
            {
                // Log ex here (NLog/Serilog/etc.). Don't leak SQL details to the client.
                return InternalServerError();
            }
        }

        private const int MaxArrayItemsScanned = 100; // caps work on huge arrays
        private const string CasBirFolder = "~/App_Data/CAS BIR";
        [HttpGet]
        [Route("api/GetDestinationColumns")]
        public IHttpActionResult GetDestinationColumns(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName) ||
                Path.GetFileName(fileName) != fileName ||
                !fileName.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                return BadRequest("fileName must be a .json file name without a path.");

            var folder = HostingEnvironment.MapPath(CasBirFolder);   // resolves "~" to the real path
            if (folder == null)
                return InternalServerError();

            var folderFull = Path.GetFullPath(folder).TrimEnd(Path.DirectorySeparatorChar)
                             + Path.DirectorySeparatorChar;

            string fullPath;
            try
            {
                fullPath = Path.GetFullPath(Path.Combine(folderFull, fileName));
            }
            catch (ArgumentException)
            {
                return BadRequest("Invalid file name.");
            }
            catch (NotSupportedException)
            {
                return BadRequest("Invalid file name.");
            }

            if (!fullPath.StartsWith(folderFull, StringComparison.OrdinalIgnoreCase))
                return BadRequest("Invalid file name.");

            if (!File.Exists(fullPath))
                return NotFound();

            try
            {
                JToken json;
                using (var stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (var sr = new StreamReader(stream))
                using (var reader = new JsonTextReader(sr))
                {
                    json = JToken.Load(reader);
                }

                var columns = new List<string>();
                Flatten(json, "", columns);

                return Ok(columns);
            }
            catch (JsonReaderException)
            {
                return BadRequest("File is not valid JSON.");
            }
            catch (UnauthorizedAccessException)
            {
                // Log it: app pool identity lacks read permission
                return InternalServerError();
            }
            catch (IOException)
            {
                // Log it: file locked or unreadable
                return InternalServerError();
            }
        }

        private static void Flatten(JToken token, string path, List<string> result)
        {
            var obj = token as JObject;
            if (obj != null)
            {
                if (!obj.HasValues) { AddUnique(result, path); return; }

                foreach (var prop in obj.Properties())
                {
                    var childPath = path.Length == 0 ? prop.Name : path + "." + prop.Name;
                    Flatten(prop.Value, childPath, result);
                }
                return;
            }

            var arr = token as JArray;
            if (arr != null)
            {
                var itemPath = path + "[]";
                if (arr.Count == 0) { AddUnique(result, itemPath); return; }

                var scanned = 0;
                foreach (var item in arr)
                {
                    if (scanned++ >= MaxArrayItemsScanned) break;
                    Flatten(item, itemPath, result);
                }
                return;
            }

            AddUnique(result, path); // primitive or null
        }

        private static void AddUnique(List<string> list, string value)
        {
            if (value.Length > 0 && !list.Contains(value))
                list.Add(value);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                _context.Dispose();
            base.Dispose(disposing);
        }



        [HttpGet]
        [Route("api/GenerateInvoiceJson")]
        public async Task<IHttpActionResult> GenerateInvoiceJson(string Code)
        {
            var proc = _context.Schedules.Where(x => x.SchedCode == Code).FirstOrDefault();
            var mapIds = await _context.Schedules
                .Where(schedule => schedule.SchedCode == Code) // Ensure we filter by the schedule code
                .Join(
                    _context.Process,
                    schedule => schedule.Process,
                    process => process.ProcessCode,
                    (schedule, process) => new { schedule, process }
                )
                .Join(
                    _context.ProcessMap,
                    sp => sp.process.ProcessId,
                    pm => pm.ProcessId,
                    (sp, pm) => pm.MapId
                ).ToArrayAsync();
            try
            {
                if (proc.RunTime == null)
                {
                    proc.RunTime = DateTime.Now.AddMinutes(-8);
                    _context.SaveChanges();
                }

                var runntime = _context.Schedules.AsEnumerable().Where(sel => sel.SchedCode == Code).Select(x => x.RunTime).FirstOrDefault();
                var currtime = DateTime.Now;

                if ((currtime.Minute - runntime.Value.Minute) > 4 || currtime.Hour != runntime.Value.Hour)
                {
                    proc.IsRunning = false;
                    _context.SaveChanges();
                }

                var isrunproc = _context.Schedules.Where(sel => sel.SchedCode == Code).Select(sel => sel.IsRunning).FirstOrDefault();

                if (isrunproc == false)
                {
                    proc.IsRunning = true;
                    proc.RunTime = DateTime.Now;
                    _context.SaveChanges();

                    var response = await eis.GenerateInvoiceJson(Code, mapIds);

                }
                proc.IsRunning = false;
                _context.SaveChanges();

                return Ok("Process completed successfully.");

            }
            catch (Exception ex)
            {

                _context.SystemLogs.Add(new DomainLayer.Models.SystemLog
                {
                    Task = "GenerateInvoiceJson",
                    CreateDate = DateTime.Now,
                    ApiUrl = "",
                    ErrorMsg = $"{ex.Message}{(ex.InnerException != null ? $" | Inner: {ex.InnerException.Message}" : "")}",
                    Json = "",
                    Database = "",
                    Table = "",
                    Module = ""
                });
                proc.IsRunning = false;
                _context.SaveChanges();
                return InternalServerError(ex);
            }

        }


    }
}