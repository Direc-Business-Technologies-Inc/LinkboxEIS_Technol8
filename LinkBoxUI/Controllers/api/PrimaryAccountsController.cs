
using ClosedXML.Excel;
using LinkBoxUI.Context;
using LinkBoxUI.Services.PrimaryAccounts;
using NPOI.SS.Formula.Functions;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;

namespace LinkBoxUI.Controllers.api
{
    [RoutePrefix("Primary")]
    public class PrimaryAccountsController : ApiController
    {
        LinkboxDb _context = new LinkboxDb();
        PrimaryAcountServices primary = new PrimaryAcountServices();

        [HttpGet]
        [Route("api/UploadPrimaryAccount")]
        public async Task<IHttpActionResult> UploadPrimaryAccount(string Code)
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

                    var response = await primary._UploadPrimaryAccount(Code,mapIds);
                 
                }
                proc.IsRunning = false;
                _context.SaveChanges();

                return Ok("Process completed successfully.");

            }
            catch (Exception ex)
            {

                _context.SystemLogs.Add(new DomainLayer.Models.SystemLog
                {
                    Task = "UploadPrimaryAccount",
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

        [HttpGet]
        [Route("api/GetFileColumns")]
        public string GetFileColumns(string filepath)
        {

            var columns = new List<string>();
            var mappingPath = Path.Combine(filepath, "Template");

            var file = Directory.GetFiles(mappingPath)
                         .FirstOrDefault(f =>
                             f.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase) ||
                             f.EndsWith(".csv", StringComparison.OrdinalIgnoreCase));

            if (file == null)
                throw new FileNotFoundException($"No Map.xlsx or Map.csv found in {mappingPath}");

            var extension = Path.GetExtension(file).ToLowerInvariant();

            var dir = file;
            if (extension.Equals(".xlsx", StringComparison.OrdinalIgnoreCase))
            {
                using (var workbook = new XLWorkbook(dir))
                {
                    var worksheet = workbook.Worksheets.First();
                    var lastColumn = worksheet.LastColumnUsed();
                    if (lastColumn == null)
                        return "";

                    for (int col = 1; col <= lastColumn.ColumnNumber(); col++)
                    {
                        columns.Add(GetExcelColumnName(col));
                    }
                }
            }
            else if (extension.Equals(".csv", StringComparison.OrdinalIgnoreCase))
            {
              
                using (var reader = new StreamReader(dir))
                {
                    string firstLine = reader.ReadLine();
                    if (string.IsNullOrEmpty(firstLine))
                        return "";

                    var fields = firstLine.Split(',');
                    for (int col = 1; col <= fields.Length; col++)
                    {
                        columns.Add(GetExcelColumnName(col));
                    }
                }
            }
            else
            {
                throw new ArgumentException($"Unsupported fileType: {extension}");
            }

            return string.Join(",", columns);
        }
        private string GetExcelColumnName(int columnNumber)
        {
            string columnName = "";
            while (columnNumber > 0)
            {
                int remainder = (columnNumber - 1) % 26;
                columnName = (char)('A' + remainder) + columnName;
                columnNumber = (columnNumber - 1) / 26;
            }
            return columnName;
        }
    }
}
