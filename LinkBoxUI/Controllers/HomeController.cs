using DataCipher;
using DomainLayer;
using DomainLayer.ViewModels;
using InfrastructureLayer.Repositories;
using LinkBoxUI.Context;
using LinkBoxUI.SessionChecker;
using Microsoft.Graph.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;

namespace LinkBoxUI.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILoginRepository _loginrepo;
        private readonly IGlobalRepository _globalrepo;

        public HomeController(ILoginRepository login, IGlobalRepository global)
        {
            _loginrepo = login;
            _globalrepo = global;
        }

        LinkboxDb db = new LinkboxDb();
        [SessionCheck]
        public ActionResult Index()
        {
            ViewBag.Title = "Home Page";
            Session["ModuleAccess"] = 0;
            return View(/*_globalrepo.GetDashBoardReport()*/);
            //return View(_globalrepo.GetItemStock());
        }

        public JsonResult GetDashBoardReport()
        {
            return Json(_globalrepo.GetDashBoardReport());
        }
        public JsonResult GetReportProgress()
        {
            return Json(_globalrepo.GetReportProgress());
        }

        public ActionResult Logout()
        {
            _loginrepo.Logout((int)Session["Id"]);
            _loginrepo.SaveChanges();
            Session.Abandon();
            Session.Clear();
            Session.RemoveAll();
            return RedirectToActionPermanent("Login", "Home");
        }
        public ActionResult Login()
        {
            _loginrepo.Login_Initialize();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Login(LoginViewModels.Login login)
        {
            if (ModelState.IsValid)
            {
                var user = _loginrepo.Get_User(login);
                ViewBag.Attempt = 0;

                if (user == null)
                {
                    ViewBag.WarningMessage = "User does not exist ";
                    ViewBag.Attempt = "";
                    ViewBag.Id = null;
                }
                else if (!user.IsActive)
                {
                    ViewBag.WarningMessage = "Inactive User";
                    ViewBag.Attempt = "";
                    ViewBag.Id = null;
                }
                else
                {
                    var userPass = _loginrepo.Check_User(login, user);
                    if (userPass == null)
                    {
                        _loginrepo.Update_Attempt(user, userPass);
                        _loginrepo.SaveChanges();
                        ViewBag.Attempt = user.Attempt;
                        Session["Attempt"] = user.Attempt;
                        Session["Id"] = user.UserId;
                        ViewBag.WarningMessage = "Incorrect Username or Password";

                    }
                    else if (userPass != null)
                    {

                        if ((user.PasswordChangedAt == null || user.PasswordChangedAt.Value.AddDays(90) < DateTime.Now) && user.AuthorizationID != 1)
                        {
                            ViewBag.WarningMessage = "Password has expired. Please contact administrator to reset your password.";
                            ViewBag.Id = null;
                        }
                        else
                        {
                            _loginrepo.Update_Attempt(user, userPass);
                            _loginrepo.SaveChanges();
                            Session["Id"] = userPass.UserId;
                            Session["UserName"] = userPass.UserName;
                            Session["Name"] = $"{userPass.FirstName} {userPass.MiddleName} {userPass.LastName}";
                            Session["CreateDate"] = userPass.CreateDate;
                            Session["LastLogin"] = userPass.LastLoginDate;

                            return RedirectToAction("Index", "Home");
                        }

                    }
                }
            }

    

            ViewBag.Title = "Login Page";
            return View(login);
        }

        public JsonResult GetModule(int Id)
        {
            return Json(_globalrepo.GetModules(Id));
        }
        public JsonResult GetItemStock(string Code)
        {
            return Json(_globalrepo.GetItemStock(Code));
        }
        //public JsonResult GetItemStock()
        //{
        //    return Json(_globalrepo.GetItemStock());
        //}

        public JsonResult SetCount()
        {
            if (Session["Id"] != null)
            {
                _loginrepo.Login_Attempt(Convert.ToInt32(Session["Id"]),
                                         Convert.ToInt32(Session["Attempt"]));
            }
            return Json(true);
        }

        public FileResult Export()
        {
            return File(_globalrepo.DataExport(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $@"Migration_report.xlsx");
            //return File("ekek","application/vnd.openxmlformats-officedocument.spreadsheetml.sheet_report.xlsx");

        }

        public JsonResult ClearCurrentRecords()
        {
            bool result = false;
            try
            {
                var errors = db.SystemLogs.Where(x => x.Task == "UPLOAD_ERROR").ToList();
                foreach (var error in errors)
                {
                    error.Task = "UPLOAD_ERROR_RESOLVED";
                    db.SaveChanges();
                }
                result = true;
                return Json(result);
            }
            catch (Exception ex)
            {
                result = false;
                return Json(result);
            }

        }
    }
}
