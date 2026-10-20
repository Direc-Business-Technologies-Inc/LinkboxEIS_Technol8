using DataCipher;
using DomainLayer;
using DomainLayer.ViewModels;
using InfrastructureLayer.Repositories;
using LinkBoxUI.Context;
using LinkBoxUI.SessionChecker;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text.RegularExpressions;
using System.Web.Mvc;

namespace LinkBoxUI.Controllers
{
    [SessionCheck]
    public class UserController : Controller
    {
        private readonly IUserRepository _repo;
        private readonly IAuthorizationRepository _authrepo;
        private readonly IGlobalRepository _global;

        private readonly LinkboxDb _context = new LinkboxDb();
        public UserController(IUserRepository repo, IAuthorizationRepository auth, IGlobalRepository global)
        {
            _repo = repo;
            _authrepo = auth;
            _global = global;
        }

        // GET: Users
        [UserDetailsCheck]
        public ActionResult UserDetails()
        {
            int currentUserID = Convert.ToInt32(Session["Id"]);

            Session["ModuleAccess"] = 1;
            ViewBag.Title = "User Details";
            return View(_repo.View_Users(currentUserID));
        }

        [UserDetailsCheck]
        public ActionResult CreateUser()
        {
            ViewBag.Title = "New User";
            ViewBag.Authorizations = new SelectList(_repo.AuthorizationList(), "AuthId", "Description");

            return View(new UserDetailsViewModel.UserViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [UserDetailsCheck]
        public ActionResult CreateUser(UserDetailsViewModel.UserViewModel model)
        {
            ViewBag.Title = "New User";
            ViewBag.Authorizations = new SelectList(_repo.AuthorizationList(), "AuthId", "Description");

            if (ModelState.IsValid)
            {
                if (_repo.IsUserExist(model.UserName))
                {
                    ViewBag.WarningMessage = "Username has already been taken";
                }
                else
                {

                    if (model.Password == model.ConfirmPassword)
                    {
                        //Check Password if its create within the last 90 days

                        //var isPasswordReused = IsPasswordReused(model.UserName, model.Password, Session["Id"].ToString()); // Assuming 0 for new user
                        //if (isPasswordReused)
                        //{
                        //    ViewBag.WarningMessage = "Password has been used in the last 3 passwords. Please choose a different password.";
                        //    return View();
                        //}

                        User user = AutoMapper.Mapper.Map<UserDetailsViewModel.UserViewModel, User>(model);
                        _repo.Create_New_User(user, Convert.ToInt32(Session["Id"]));
                        _repo.SaveChanges();
                        ModelState.Clear();
                        ViewBag.SuccessMessage = "Successfully Created";
                        return RedirectToAction("UserDetails", "User");
                    }
                    else
                    {
                        ViewBag.WarningMessage = "Password not match";
                        return View();
                    }
                }
            }
            return View();
        }

        private static string ComputeReuseHash(string password)
        {
            using (var sha256 = System.Security.Cryptography.SHA256.Create())
            {
                var bytes = System.Text.Encoding.UTF8.GetBytes(password);
                var hash = sha256.ComputeHash(bytes);
                return Convert.ToBase64String(hash);
            }
        }

        private bool IsPasswordReused(int userId, string newPassword)
        {
            var newReuseHash = Cryption.Encrypt(newPassword);

            return _context.PasswordHistories
                .Where(x => x.UserId == userId)
                .OrderByDescending(x => x.CreatedAt)
                .Take(3)
                .Select(x => x.PasswordHash)   // this column now stores the reuse hash, not Cryption.Encrypt output
                .Contains(newReuseHash);
        }

        [UserDetailsCheck]
        public JsonResult FindUser(int Id)
        {
            return Json(_repo.Find_User(Id));
        }
        [UserDetailsCheck]
        public JsonResult FindAPIKey(int Id)
        {
            return Json(_repo.Fine_Key(Id));
        }

        [UserDetailsCheck]
        public JsonResult Update_User(int Id, string UserName,
                                string FirstName, string LastName,
                                string MiddleName, bool IsActive, int AuthId = 1,
                                string Password = null , string ConfirmPassword = null)
        {
            bool isChangingPassword = !string.IsNullOrEmpty(Password);
        
            if (isChangingPassword)
            {
                //check the vakudation first on 

                if (Password.Length < 8)
                    return Json(new { success = false, message = "Password must be at least 8 characters long." });

                if (!Regex.IsMatch(Password, @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^\da-zA-Z]).+$"))
                    return Json(new { success = false, message = "Password must contain uppercase, lowercase, a number, and a special character." });

                // Confirm-password check — this was entirely missing before
                if (string.IsNullOrEmpty(ConfirmPassword))
                    return Json(new { success = false, message = "Please confirm your new password." });

                if (Password != ConfirmPassword)
                    return Json(new { success = false, message = "Passwords do not match." });

                var isPasswordReused = IsPasswordReused(Id, UserName + Password);
                if (isPasswordReused)
                {
                    return Json(new { success = false, message = "This password has been used in your last 3 passwords. Please enter a different password." }, JsonRequestBehavior.AllowGet);
                }
            }
            _repo.Update_User(Id, UserName, FirstName, LastName,
                               MiddleName, IsActive, Convert.ToInt32(Session["Id"]), AuthId,Password,
                               ConfirmPassword);

            return Json(new { success = true, message = "" }, JsonRequestBehavior.AllowGet);
        }
        [UserDetailsCheck]
        public JsonResult FindUserModule(int Id)
        {
            return Json(_repo.GetUserModules(Id));
        }

        [UserDetailsCheck]
        public JsonResult UpdateUserModule(int Id, int[] activeMods = null, int[] UMId = null, int[] inactiveMods = null)
        {
            activeMods = activeMods ?? new int[0];
            inactiveMods = inactiveMods ?? new int[0];
            UMId = UMId ?? new int[0];
            List<int> actives = activeMods.ToList();
            List<int> temp = new List<int>();
            List<int> distinct = new List<int>();

            if (activeMods == null)
            {
                foreach (var x in UMId)
                {
                    if (_repo.isActiveModule(x))
                    {
                        _repo.UpdateUserModules(Convert.ToInt32(Session["Id"]), x, false);
                    }
                }
            }

            if (inactiveMods == null)
            {
                foreach (var x in UMId)
                {
                    foreach (var y in activeMods)
                    {

                        if (_repo.GetModuleId(x) == y)
                        {
                            temp.Add(y);
                            if (!_repo.isActiveModule(x))
                            {
                                _repo.UpdateUserModules(Convert.ToInt32(Session["Id"]), x, true);
                            }
                        }

                    }
                }
                actives = _repo.NewModuleList(temp, actives);
                distinct = actives.Distinct().ToList();
                foreach (var x in distinct)
                {
                    _repo.AddUserModules(Convert.ToInt32(Session["Id"]), Id, x);
                }
            }

            if (UMId == null)
            {
                foreach (var x in activeMods)
                {
                    _repo.AddUserModules(Convert.ToInt32(Session["Id"]), Id, x);
                }
            }

            foreach (var x in UMId)
            {
                foreach (var y in activeMods)
                {
                    if (_repo.GetModuleId(x) == y)
                    {
                        temp.Add(y);
                        if (!_repo.isActiveModule(x))
                        {
                            _repo.UpdateUserModules(Convert.ToInt32(Session["Id"]), x, true);
                        }
                    }

                }
            }
            foreach (var x in UMId)
            {
                foreach (var y in inactiveMods)
                {
                    if (_repo.GetModuleId(x) == y)
                    {
                        if (_repo.isActiveModule(x))
                        {
                            _repo.UpdateUserModules(Convert.ToInt32(Session["Id"]), x, false);
                        }
                    }

                }
            }
            actives = _repo.NewModuleList(temp, actives);
            distinct = actives.Distinct().ToList();
            foreach (var x in distinct)
            {
                _repo.AddUserModules(Convert.ToInt32(Session["Id"]), Id, x);
            }
            return Json(true);
        }

        [AuthorizationCheck]
        public ActionResult AuthList()
        {
            Session["ModuleAccess"] = 2;
            ViewBag.Title = "Authorization";
            return View(_authrepo.View_Authorization());
        }

        [AuthorizationCheck]
        public JsonResult CreateAuthorization(Authorizations.AuthorizationViewModel model)
        {
            Authorization authorization = AutoMapper.Mapper.Map<Authorizations.AuthorizationViewModel, Authorization>(model);
            return Json(_authrepo.Create_Authorization(authorization, Convert.ToInt32(Session["Id"])));
        }
        [AuthorizationCheck]
        public JsonResult FindAuth(int Id)
        {          
            return Json(_authrepo.Find_Authorization(Id));
        }

        [AuthorizationCheck]
        public JsonResult UpdateAuthorization(Authorizations.AuthorizationViewModel model)
        {
            Authorization authorization = AutoMapper.Mapper.Map<Authorizations.AuthorizationViewModel, Authorization>(model);
            return Json(_authrepo.Update_Authorization(authorization, Convert.ToInt32(Session["Id"])));
        }

        [AuthorizationCheck]
        public JsonResult FindAuthModule(int Id)
        {
            return Json(_authrepo.Get_AuthorizationModules(Id));
        }

        [AuthorizationCheck]
        public JsonResult UpdateAuthModule(int Id, int[] activeMods = null, int[] AMId = null, int[] inactiveMods = null)
        {
            activeMods = activeMods ?? new int[0];
            inactiveMods = inactiveMods ?? new int[0];
            AMId = AMId ?? new int[0];
            List<int> actives = activeMods.ToList();
            List<int> temp = new List<int>();
            List<int> distinct = new List<int>();

            if (activeMods == null)
            {
                foreach (var x in AMId)
                {
                    if (_authrepo.isActiveModule(x))
                    {
                        _authrepo.Update_AuthorizationModules(Convert.ToInt32(Session["Id"]), x, false);
                    }
                }
            }

            if (inactiveMods == null)
            {
                foreach (var x in AMId)
                {
                    foreach (var y in activeMods)
                    {

                        if (_authrepo.GetModuleId(x) == y)
                        {
                            temp.Add(y);
                            if (!_authrepo.isActiveModule(x))
                            {
                                _authrepo.Update_AuthorizationModules(Convert.ToInt32(Session["Id"]), x, true);
                            }
                        }

                    }
                }
                actives = _authrepo.NewModuleList(temp, actives);
                distinct = actives.Distinct().ToList();
                foreach (var x in distinct)
                {
                    _authrepo.Add_AuthorizationModules(Convert.ToInt32(Session["Id"]), Id, x);
                }
            }

            if (AMId == null)
            {
                foreach (var x in activeMods)
                {
                    _authrepo.Add_AuthorizationModules(Convert.ToInt32(Session["Id"]), Id, x);
                }
            }

            foreach (var x in AMId)
            {
                foreach (var y in activeMods)
                {
                    if (_authrepo.GetModuleId(x) == y)
                    {
                        temp.Add(y);
                        if (!_repo.isActiveModule(x))
                        {
                            _authrepo.Update_AuthorizationModules(Convert.ToInt32(Session["Id"]), x, true);
                        }
                    }

                }
            }

            foreach (var x in AMId)
            {
                foreach (var y in inactiveMods)
                {
                    if (_authrepo.GetModuleId(x) == y)
                    {
                        if (_authrepo.isActiveModule(x))
                        {
                            _authrepo.Update_AuthorizationModules(Convert.ToInt32(Session["Id"]), x, false);
                        }
                    }

                }
            }
            actives = _authrepo.NewModuleList(temp, actives);
            distinct = actives.Distinct().ToList();
            foreach (var x in distinct)
            {
                _authrepo.Add_AuthorizationModules(Convert.ToInt32(Session["Id"]), Id, x);
            }
            return Json(true);
        }
        public JsonResult GetModule(int Id)
        { 
            return Json(_global.GetModules(Id));
        }

    }
}