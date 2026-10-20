using DataCipher;
using DomainLayer;
using DomainLayer.ViewModels;
using InfrastructureLayer.Repositories;
using LinkBoxUI.Context;
using Microsoft.Graph.Models;
using Microsoft.Graph.Models.CallRecords;
using Remotion.Data.Linq.Clauses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using static DomainLayer.ViewModels.SetupCreateViewModel;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.StartPanel;
namespace LinkBoxUI.Services
{
    public class UserServices : IUserRepository
    {
        private readonly LinkboxDb _context = new LinkboxDb();
        public void Create_New_User(DomainLayer.User user, int userid)
        {
            var plainPassword = user.UserName + user.Password;
            user.Password = Cryption.Encrypt(user.UserName + user.Password);
            user.UserAPIKey = Cryption.Encrypt(user.UserName + user.FirstName + (string.IsNullOrEmpty(user.MiddleName) ? "" : user.MiddleName));
            user.IsActive = true;
            user.CreateUserID = userid;
            user.CreateDate = DateTime.Now;
            user.PasswordChangedAt = DateTime.Now;
            _context.Users.Add(user);
            _context.SaveChanges();

            _context.PasswordHistories.Add(new DomainLayer.Models.PasswordHistoryModel
            {
                UserId = user.UserId,         // change to user.UserID if that's your actual PK name
                PasswordHash = Cryption.Encrypt(plainPassword),
                CreatedAt = DateTime.Now
            });
            _context.SaveChanges();

            //SaveUserModules(user.AuthorizationID, userid);
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


        public void Update_User(int Id, string UserName, 
                                string FirstName, string LastName, 
                                string MiddleName, bool IsActive, int userid, int AuthId,
                                string Password = null, string LastPassword = null)
        {


            var ousr = _context.Users.Find(Id);
            ousr.UserName = UserName;
            ousr.FirstName = FirstName;
            ousr.LastName = LastName;
            ousr.MiddleName = MiddleName;
            ousr.IsActive = IsActive;
           
            ousr.UpdateDate = DateTime.Now;
            var NewPassword = UserName + Password;
            if (!string.IsNullOrEmpty(Password))
            { 
                ousr.PasswordChangedAt = DateTime.Now;
                ousr.Password = Cryption.Encrypt(NewPassword);
                ousr.LastPassword = Cryption.Encrypt(LastPassword);

            }
            ousr.AuthorizationID = AuthId;
            ousr.UpdateUserID = userid;
            _context.Entry(ousr).State = System.Data.Entity.EntityState.Modified;
            _context.SaveChanges();

            if (!string.IsNullOrEmpty(Password))
            {
                _context.PasswordHistories.Add(new DomainLayer.Models.PasswordHistoryModel
                {
                    UserId = Id,         // change to user.UserID if that's your actual PK name
                    PasswordHash = Cryption.Encrypt(NewPassword),
                    CreatedAt = DateTime.Now
                });
                _context.SaveChanges();
            }
                
        }
        public UserDetailsViewModel Find_User(int id)
        {
            var model = new UserDetailsViewModel();
            model.CreateUser = _context.Users.Where(x => x.UserId == id).Select(x => new UserDetailsViewModel.UserViewModel

            {
                UserName = x.UserName,
                FirstName = x.FirstName,
                LastName = x.LastName,
                MiddleName = x.MiddleName,
                Password = x.Password,
                ConfirmPassword = x.Password,
                LastPassword = x.LastPassword,
                IsActive = x.IsActive,
                AuthorizationId = x.AuthorizationID,
            }).ToList();
            //var result = model.CreateUser.ToList();
            //foreach (var x in result)
            //{

            //    x.Password = Cryption.Decrypt(x.Password);
            //    x.ConfirmPassword = Cryption.Decrypt(x.ConfirmPassword);
            //    if (x.LastPassword != null)
            //    {
            //        x.LastPassword = Cryption.Decrypt(x.LastPassword);
            //    }
            //}

            model.Auths = _context.Authorizations
            .Where(x => x.AuthId != 1)
            .Select(x => new UserDetailsViewModel.AuthorizationViewModel 
                {
                    AuthId = x.AuthId,
                    Description = x.Description
    
            }).ToList();

            return model;
        }
        public List<UserDetailsViewModel.UserViewModel> Fine_Key(int id)
        {
            var model = new UserDetailsViewModel();
            model.CreateUser = _context.Users.Where(x => x.UserId == id).Select(x => new UserDetailsViewModel.UserViewModel
            {
                UserAPIKey = string.IsNullOrEmpty(x.UserAPIKey) ? "N/A" : x.UserAPIKey,
            }).ToList();
            return model.CreateUser;
        }
        public UserDetailsViewModel View_Users(int UserId)
        {
            UserDetailsViewModel user = new UserDetailsViewModel();

            //user.CreateUser = _context.Users.Where(x => x.UserId != 1)
            //    .Join(_context.Authorizations).Select(x => AuthorizationID is equal to
            //        new UserDetailsViewModel.UserViewModel
            //        {
            //            UserId = x.UserId,
            //            UserName = x.UserName,
            //            FirstName = x.FirstName,
            //            LastName = x.LastName,
            //            MiddleName = x.MiddleName,
            //            UserAPIKey = x.UserAPIKey,
            //            IsActive = x.IsActive,
            //            AuthorizationName = x
            //        }).ToList();


            var isSuperAdmin = _context.Users.Any(x => x.UserId == UserId && x.AuthorizationID == 1);

            user.CreateUser = _context.Users
                     
                    .GroupJoin(_context.Authorizations,
                        u => u.AuthorizationID,
                        a => a.AuthId,
                        (u, auths) => new { u, auth = auths.FirstOrDefault() })
                    .Select(x => new UserDetailsViewModel.UserViewModel
                    {
                        UserId = x.u.UserId,
                        UserName = x.u.UserName,
                        FirstName = x.u.FirstName,
                        LastName = x.u.LastName,
                        MiddleName = x.u.MiddleName,
                        UserAPIKey = x.u.UserAPIKey,
                        IsActive = x.u.IsActive,
                        AuthorizationId = x.auth != null ? x.auth.AuthId : 0,
                        AuthorizationName = x.auth != null ? x.auth.Description : null,
                    })
                    .ToList();

            user.ModulesList = _context.Modules.Where(x => x.IsActive == true).Select(sel =>
                    new UserDetailsViewModel.Modules
                    {
                        ModName = sel.ModName,
                        ModId = sel.ModId,
                        ModCode = sel.ModuleCode
                    }).ToList();

            user.IsAdmin = isSuperAdmin;
            return user;
        }
        public IEnumerable<Authorization> AuthorizationList()
        {
            return _context.Authorizations.Where(x => x.IsActive == true && x.AuthId !=1);

        }
        public bool IsUserExist(string username)
        {
            return _context.Users.Where(e => e.UserName == username).Any();
        }
        public void SaveUserModules(int id, int userid)
        {
            var modules = _context.Users.Join(_context.Authorizations.Where(b => b.AuthId == id), a => a.AuthorizationID, b => b.AuthId, (a, b) => new { a, b })
                                        .Join(_context.AuthorizationModules.Where(c => c.IsActive == true), ab => ab.b.AuthId, c => c.AuthId, (ab, c) => new { ab, c })
                                        .Join(_context.Modules, abc => abc.c.ModId, d => d.ModId, (abc, d) => new { abc.ab.a.UserId, d.ModId }).ToList();
            foreach (var x in modules)
            {
                _context.UserModules.Add(new UserModule()
                {
                    UserId = x.UserId,
                    ModId = x.ModId,
                    IsActive = true,
                    CreateDate = DateTime.Now,
                    CreateUserID = userid
                });
                _context.SaveChanges();
            }
        }
        public UserDetailsViewModel GetUserModules(int id)
        {
            var model = new UserDetailsViewModel();

            model.UserModuleList = _context.UserModules.Where(a => a.UserId == id).Join(_context.Users, a => a.UserId, b => b.UserId, (a, b) => new { a, b })
                                           .Join(_context.Modules.Where(c => c.IsActive == true), ab => ab.a.ModId, c => c.ModId, (ab, c) => new UserDetailsViewModel.UserModules
                                           {
                                               UMId = ab.a.Id,
                                               UserName = ab.b.UserName,
                                               UserId = ab.b.UserId,
                                               ModId = c.ModId,
                                               ModuleName = c.ModName,
                                               ModuleCode = c.ModuleCode,
                                               IsActive = ab.a.IsActive
                                           }).ToList();

            model.ModulesList = _context.Modules.Where(x => x.IsActive == true).Select(sel => new UserDetailsViewModel.Modules
            {
                ModName = sel.ModName,
                ModId = sel.ModId,
                ModCode = sel.ModuleCode
            }).ToList();
            return model;
        }
        public bool isActiveModule(int id)
        {
            var modules = _context.UserModules.Find(id);
            return modules.IsActive;
        }
        public void UpdateUserModules(int userid, int moduleid, bool isactive)
        {
            var modules = _context.UserModules.Find(moduleid);
            modules.IsActive = isactive;
            modules.UpdateDate = DateTime.Now;
            modules.UpdateUserID = userid;
            _context.Entry(modules).State = System.Data.Entity.EntityState.Modified;
            _context.SaveChanges();
        }
        public int GetModuleId(int id)
        {
            var modules = _context.UserModules.Find(id);
            return modules.ModId;
        }
        public List<int> NewModuleList(List<int> olddata,List<int> newdata)
        {
            foreach (var x in olddata)
            {
                newdata.Remove(x);
            }
            return newdata;
        }
        public void AddUserModules(int createid,int userid,int moduleid)
        {
            var module = new UserModule();
            module.UserId = userid;
            module.ModId = moduleid;
            module.IsActive = true;
            module.CreateDate = DateTime.Now;
            module.CreateUserID = createid;
            _context.UserModules.Add(module);
            _context.SaveChanges();
        }
        public int SaveChanges()
        {
            return _context.SaveChanges();
        }


    }
}