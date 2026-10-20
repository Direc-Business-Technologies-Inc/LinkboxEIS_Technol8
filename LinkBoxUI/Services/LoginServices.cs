using DataCipher;
using DomainLayer;
using InfrastructureLayer.Repositories;
using LinkBoxUI.Context;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using DomainLayer.ViewModels;

namespace LinkBoxUI.Services
{
    public class LoginServices : ILoginRepository
    {
        private readonly LinkboxDb _context = new LinkboxDb();

        public User Check_User(LoginViewModels.Login login, User user)
        {
            var decrypted = Cryption.Decrypt(user.Password);
            return _context.Users.Where(a => a.UserName == login.userName && decrypted == login.userName + login.password).FirstOrDefault();
        }

        public User Get_User(LoginViewModels.Login login)
        {
            return _context.Users.Where(a => a.UserName == login.userName).FirstOrDefault();
        }

        public void Login_Attempt(int id, int count)
        {
            if (count >= 3)
            {
                var user = (_context.Users.Where(a => a.UserId == id).FirstOrDefault());
                user.Attempt = 0;
                _context.SaveChanges();
                SaveChanges();
            }
        }

        public void Login_Initialize()
        {
            if (!_context.Users.Any())
            {
                _context.Users.Add(new User()
                {
                    UserName = "admin",
                    FirstName = "administrator",
                    LastName = "admin",
                    Password = Cryption.Encrypt("admin" + "admin"),
                    IsActive = true,
                    CreateDate = DateTime.Now,
                    CreateUserID = 1,
                    Attempt = 0,
                    AuthorizationID = 1,

                });
                _context.SaveChanges();
                SaveChanges();
            }

            if (!_context.Modules.Any())
            {
                string[] modCode = { "UD", "AUT", "SU", "FM", "DEP", "ESU", "PSU", "UPS", "SY", "UP", "QUERY" };
                string[] modName = { "User Details", "Authorizations", "Setup", "Field Mapping", "Deposit", "Email Setup", "Process Setup", "Upload Sched", "Sync", "Uploader", "Query Manager" };

                bool isActive = true;
                DateTime createDate = DateTime.Now;
                int createId = 1;

                for (int x = 0; x < modName.Length; x++)
                {
                    _context.Modules.Add(new Module()
                    {
                        ModuleCode = modCode[x],
                        ModName = modName[x],
                        IsActive = isActive,
                        CreateDate = createDate,
                        CreateUserID = createId
                    });
                    _context.SaveChanges();
                }
                SaveChanges();
            }

            if (!_context.Authorizations.Any())
            {
                _context.Authorizations.Add(new Authorization()
                {
                    AuthCode = "AD",
                    Description = "Admin",
                    IsActive = true,
                    CreateDate = DateTime.Now,
                    CreateUserID = 1
                });
                _context.SaveChanges();
                SaveChanges();
            }

            if (!_context.AuthorizationModules.Any())
            {
                for (int x = 0; x < 11; x++)
                {
                    _context.AuthorizationModules.Add(new AuthorizationModule()
                    {
                        AuthId = 1,
                        ModId = x + 1,
                        IsActive = true,
                        CreateDate = DateTime.Now,
                        CreateUserID = 1
                    });
                    _context.SaveChanges();
                }
                SaveChanges();
            }

            if (!_context.UserModules.Any())
            {
                for (int x = 0; x < 11; x++)
                {
                    _context.UserModules.Add(new UserModule()
                    {
                        UserId = 1,
                        ModId = x + 1,
                        IsActive = true,
                        CreateDate = DateTime.Now,
                        CreateUserID = 1
                    });
                    _context.SaveChanges();
                }
                SaveChanges();
            }


        }

        public void Logout(int id)
        {
            var user = (_context.Users.Where(a => a.UserId == id).FirstOrDefault());
            user.LastLoginDate = DateTime.Now;
            _context.SaveChanges();
        }

        public int SaveChanges()
        {
            return _context.SaveChanges();
        }

        public void Update_Attempt(User user, User userpass)
        {
            if (userpass == null)
            {
                user.Attempt += 1;
            }
            else
            {
                user.Attempt =0;
            }
            _context.SaveChanges();
        }
    }
}