using DocumentFormat.OpenXml.EMMA;
using DomainLayer.Models;
using DomainLayer.ViewModels;
using LinkBoxUI.Context;
using LinkBoxUI.SessionChecker;
using StructureMap.Query;
using System;
using System.Data.Entity;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web.Mvc;

namespace LinkBoxUI.Controllers
{
    [SessionCheck]
    public class ErrorLogsController : Controller
    {
        // GET: ErrorLogs
        //LinkboxDb db = new LinkboxDb();
        //public ActionResult Index()
        //{
        //    ErrorLogsViewModel model = new ErrorLogsViewModel();
        //    //x.CreateDate > DateTime.Today

        //    //.Where(x=>x.Task.ToLower().Contains("ERROR"))
        //    var platforms = new[] { "lazada", "tiktok", "shopee" };
        //    model.PlatformErrorList = db.SystemLogs
        //    .Where(x =>
        //        platforms.Any(p => x.Task.ToLower().Contains(p)) &&
        //        x.Module != null &&
        //        x.Module != ""
        //    )
        //      .Select(x => new ErrorLogsViewModel.ErrorsViewModel
        //      {
        //          id = x.Id,
        //          Task = x.Task,
        //          ErrorMsg = x.ErrorMsg,
        //          Database = x.Database == "1" ? "Sent" : x.Database,
        //          Module = x.Module,
        //          Table = x.Table,
        //          CreateDate = x.CreateDate,
        //      })
        //      .OrderByDescending(x => x.id)
        //      .ToList();

        //    model.PrimaryErrorList = db.SystemLogs
        //        .Where(x => x.Task.ToLower().Contains("excel to sap"))
        //        .Select(x => new ErrorLogsViewModel.ErrorsViewModel
        //        {
        //            id = x.Id,
        //            Task = x.Task,
        //            ErrorMsg = x.ErrorMsg,
        //            Database = x.Database == "1" ? "Sent" : x.Database,
        //            Module = x.Module,
        //            Table = x.Table,
        //            CreateDate = x.CreateDate,
        //        })
        //        .OrderByDescending(x => x.id)
        //        .ToList();

        //    model.SystemErrorList = db.SystemLogs
        //        .Where(x => !platforms.Any(p => x.Task.ToLower().Contains(p))
        //                 && !x.Task.ToLower().Contains("excel to sap"))
        //        .Select(x => new ErrorLogsViewModel.ErrorsViewModel
        //        {
        //            id = x.Id,
        //            Task = x.Task,
        //            ErrorMsg = x.ErrorMsg,
        //            Database = x.Database == "1" ? "Sent" : x.Database,
        //            Module = x.Module,
        //            Table = x.Table,
        //            CreateDate = x.CreateDate,
        //        })
        //        .OrderByDescending(x => x.id)
        //        .ToList();


        //    return View(model);
        //}
        private readonly LinkboxDb db = new LinkboxDb();

        public ActionResult Index() => View();   // no data loaded here anymore
        [HttpPost]
        public ActionResult GetErrors(string category, int draw, int start, int length, string search, string orderDir)
        {
            length = Math.Min(Math.Max(length, 1), 500);
            start = Math.Max(start, 0);

            IQueryable<SystemLog> baseQuery = db.SystemLogs.AsNoTracking();

            switch (category)
            {
                case "platform":
                    baseQuery = baseQuery.Where(x =>
                        (x.Task.Contains("lazada") || x.Task.Contains("tiktok") || x.Task.Contains("shopee"))
                        && x.Module != null && x.Module != "");
                    break;
                case "primary":
                    baseQuery = baseQuery.Where(x => x.Task.Contains("excel to sap"));
                    break;
                case "other":
                    baseQuery = baseQuery.Where(x =>
                        !x.Task.Contains("lazada") && !x.Task.Contains("tiktok") &&
                        !x.Task.Contains("shopee") && !x.Task.Contains("excel to sap"));
                    break;
                default:
                    Response.StatusCode = 400;   // let DataTables' error handler fire
                    return Json(new { error = "Unknown category" });
            }

            var total = baseQuery.Count();

            var filtered = baseQuery;
            var term = (search ?? string.Empty).Trim();
            if (term.Length > 0)
            {
                if (term.Length > 200) term = term.Substring(0, 200);

                int id;
                bool hasId = int.TryParse(term, out id);          // adjust type if Id is long
                DateTime dateFrom, dateTo;
                bool hasDate = TryParseDateRange(term, out dateFrom, out dateTo);

                filtered = filtered.Where(x =>
                    x.Task.Contains(term) ||
                    x.ErrorMsg.Contains(term) ||
                    x.Module.Contains(term) ||
                    (hasId && x.Id == id) ||
                    (hasDate && x.CreateDate >= dateFrom && x.CreateDate < dateTo));
            }

            var filteredCount = filtered.Count();

            var isAsc = string.Equals(orderDir, "asc", StringComparison.OrdinalIgnoreCase);

            // Id tiebreaker keeps paging stable when CreateDate values collide.
            var ordered = isAsc
                ? filtered.OrderBy(x => x.CreateDate).ThenBy(x => x.Id)
                : filtered.OrderByDescending(x => x.CreateDate).ThenByDescending(x => x.Id);

            var rows = ordered
                .Skip(start).Take(length)
                .Select(x => new { x.Id, x.Task, x.ErrorMsg, x.CreateDate, x.Module })
                .ToList()
                .Select(x => new
                {
                    id = x.Id,
                    task = x.Task,
                    errorMsg = x.ErrorMsg,
                    createDate = x.CreateDate.ToString("yyyy-MM-dd HH:mm:ss"),
                    module = x.Module
                });

            return Json(new { draw, recordsTotal = total, recordsFiltered = filteredCount, data = rows });
        }

        // Only accepts the format the grid displays, so "14:30" or "2026-09" don't produce surprise matches.
        private static bool TryParseDateRange(string term, out DateTime from, out DateTime to)
        {
            var ci = CultureInfo.InvariantCulture;
            DateTime d;

            if (DateTime.TryParseExact(term, "yyyy-MM-dd", ci, DateTimeStyles.None, out d))
            { from = d; to = d.AddDays(1); return true; }

            if (DateTime.TryParseExact(term, "yyyy-MM-dd HH:mm", ci, DateTimeStyles.None, out d))
            { from = d; to = d.AddMinutes(1); return true; }

            if (DateTime.TryParseExact(term, "yyyy-MM-dd HH:mm:ss", ci, DateTimeStyles.None, out d))
            { from = d; to = d.AddSeconds(1); return true; }

            from = to = default(DateTime);
            return false;
        }
        //[HttpPost]
        //public JsonResult GetErrors(string category, int draw, int start, int length, string search)
        //{
        //    length = Math.Min(Math.Max(length, 1), 500);   // never trust client page size

        //    IQueryable<SystemLog> baseQuery = db.SystemLogs.AsNoTracking();

        //    switch (category)
        //    {
        //        case "platform":
        //            baseQuery = baseQuery.Where(x =>
        //                (x.Task.Contains("lazada") || x.Task.Contains("tiktok") || x.Task.Contains("shopee"))
        //                && x.Module != null && x.Module != "");
        //            break;
        //        case "primary":
        //            baseQuery = baseQuery.Where(x => x.Task.Contains("excel to sap"));
        //            break;
        //        case "other":
        //            baseQuery = baseQuery.Where(x =>
        //                !x.Task.Contains("lazada") && !x.Task.Contains("tiktok") &&
        //                !x.Task.Contains("shopee") && !x.Task.Contains("excel to sap"));
        //            break;
        //        default:
        //            return Json(new { error = "Unknown category" });
        //    }

        //    var total = baseQuery.Count();

        //    var filtered = baseQuery;
        //    if (!string.IsNullOrWhiteSpace(search))
        //        filtered = filtered.Where(x => x.Task.Contains(search) || x.ErrorMsg.Contains(search)
        //                                    || x.Module.Contains(search));

        //    var filteredCount = filtered.Count();

        //    var rows = filtered
        //        .OrderByDescending(x => x.Id)
        //        .Skip(start).Take(length)
        //        .Select(x => new { x.Id, x.Task, x.ErrorMsg, x.CreateDate, x.Module })
        //        .ToList()   // materialize page only, then format dates in memory
        //        .Select(x => new
        //        {
        //            id = x.Id,
        //            task = x.Task,
        //            errorMsg = x.ErrorMsg,
        //            createDate = x.CreateDate.ToString("yyyy-MM-dd HH:mm:ss"),
        //            module = x.Module
        //        });

        //    return Json(new { draw, recordsTotal = total, recordsFiltered = filteredCount, data = rows });
        //}
        //[HttpPost]
        //public JsonResult GetErrors(string category, int draw, int start, int length, string search, string orderDir)
        //{
        //    length = Math.Min(Math.Max(length, 1), 500);   // never trust client page size

        //    IQueryable<SystemLog> baseQuery = db.SystemLogs.AsNoTracking();

        //    switch (category)
        //    {
        //        case "platform":
        //            baseQuery = baseQuery.Where(x =>
        //                (x.Task.Contains("lazada") || x.Task.Contains("tiktok") || x.Task.Contains("shopee"))
        //                && x.Module != null && x.Module != "");
        //            break;
        //        case "primary":
        //            baseQuery = baseQuery.Where(x => x.Task.Contains("excel to sap"));
        //            break;
        //        case "other":
        //            baseQuery = baseQuery.Where(x =>
        //                !x.Task.Contains("lazada") && !x.Task.Contains("tiktok") &&
        //                !x.Task.Contains("shopee") && !x.Task.Contains("excel to sap"));
        //            break;
        //        default:
        //            return Json(new { error = "Unknown category" });
        //    }

        //    var total = baseQuery.Count();

        //    var filtered = baseQuery;
        //    if (!string.IsNullOrWhiteSpace(search))
        //        filtered = filtered.Where(x => x.Task.Contains(search) || x.ErrorMsg.Contains(search)
        //                                    || x.Module.Contains(search));

        //    var filteredCount = filtered.Count();

        //    // Only Create Date is sortable (client restricts it via columnDefs), so a single
        //    // flat "asc"/"desc" flag is enough — no need to bind a DataTables order[] array.
        //    var isAsc = orderDir == "asc";

        //    var ordered = isAsc
        //        ? filtered.OrderBy(x => x.CreateDate)
        //        : filtered.OrderByDescending(x => x.CreateDate);

        //    var rows = ordered
        //        .Skip(start).Take(length)
        //        .Select(x => new { x.Id, x.Task, x.ErrorMsg, x.CreateDate, x.Module })
        //        .ToList()   // materialize page only, then format dates in memory
        //        .Select(x => new
        //        {
        //            id = x.Id,
        //            task = x.Task,
        //            errorMsg = x.ErrorMsg,
        //            createDate = x.CreateDate.ToString("yyyy-MM-dd HH:mm:ss"),
        //            module = x.Module
        //        });

        //    return Json(new { draw, recordsTotal = total, recordsFiltered = filteredCount, data = rows });
        //}

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }

    }


}