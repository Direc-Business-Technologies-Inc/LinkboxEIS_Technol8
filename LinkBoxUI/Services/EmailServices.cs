using LinkBoxUI.Context;
using InfrastructureLayer.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using DomainLayer.ViewModels;
using DomainLayer;

namespace LinkBoxUI.Services
{
    public class EmailServices : IEmailRepository
    {
        private readonly LinkboxDb _context = new LinkboxDb();

        public void Create_EmailSetup(EmailLogs email, string code, string[] cc, int id)
        {
            var recipients = "";
            foreach (var item in cc)
            {
                recipients += item + ",";
            }
            var AddonId = _context.FieldMappings.Where(x => x.MapCode == code).FirstOrDefault();
            email.EmailCc = recipients.Remove(recipients.Length, 0);
            email.AddonId = Convert.ToInt32(AddonId.MapId);
            email.IsActive = true;
            email.CreateDate = DateTime.Now;
            email.CreateUserID = id;
            _context.Emails.Add(email);
            _context.SaveChanges();
        }

        public void Create_EmailTemplate(EmailCreateViewModel.EmailTemplate email, int id)
        {
            EmailTemplate model = new EmailTemplate();
            model.Body = email.Body;
            model.Code = email.Code;
            model.CredCode = email.CredCode;
            model.CreateUserID = id;
            model.Description = email.Description;
            model.IsActive = email.IsActive;
            model.CreateDate = DateTime.Now;
            model.QueryCode = email.QueryCode;
            model.To = email.To != null && email.To.Length != 0 ? string.Join(",", email.To) : "";
            model.CC = email.CC != null && email.CC.Length != 0 ? string.Join(",", email.CC) : "";
            model.FileCode = email.FileCode;
            model.Subject = email.Subject;
            model.QueryCC = email.QueryCC;
            model.QueryTo = email.QueryTo;
            model.IsActive = true;
            model.FileCode = email.FileCode;
            model.Company = email.Company;
            _context.EmailTemplate.Add(model);
            _context.SaveChanges();
        }
        public void Update_EmailTemplate(EmailCreateViewModel.EmailTemplate email, int id)
        {
            var model = _context.EmailTemplate.Where(x => x.Code == email.Code).FirstOrDefault();
            model.Body = email.Body;
            model.Code = email.Code;
            model.CredCode = email.CredCode;
            model.CreateUserID = id;
            model.Description = email.Description;
            model.IsActive = email.IsActive;
            model.UpdateDate = DateTime.Now;
            model.QueryCode = email.QueryCode;
            model.To = email.To !=null && email.To.Length != 0 ? string.Join(",", email.To) : "";
            model.CC = email.CC != null && email.CC.Length != 0 ? string.Join(",", email.CC) : "";
            model.FileCode = email.FileCode;
            model.Subject = email.Subject;
            model.QueryCC = email.QueryCC;
            model.QueryTo = email.QueryTo;
            model.UpdateUserID = id;
            model.FileCode = email.FileCode;
            model.Company = email.Company;
            _context.Entry(model).State = System.Data.Entity.EntityState.Modified;
            _context.SaveChanges();
        }

        public EmailCreateViewModel Find_EmailSetup(int id)
        {
            var model = new EmailCreateViewModel();

            model.EmailView = _context.Emails.Where(x => x.EmailId == id).Select((x) => new EmailCreateViewModel.Email
            {
                EmailCode = x.EmailCode,
                EmailFrom = x.EmailFrom,
                EmailPassword = x.EmailPassword,
                EmailSubject = x.EmailSubject,
                EmailHost = x.EmailHost,
                EmailPort = x.EmailPort,
                EmailSsl = x.EmailSsl,
                EmailTo = x.EmailTo,
                EmailCc = x.EmailCc,
                EmailLog = x.EmailLog,
                IsActive = x.IsActive,
                AddonId = x.AddonId
            }).ToList();

            var emailcc = model.EmailView.Select(x => x.EmailCc).FirstOrDefault().Split(',');
            int i = model.EmailView[0].AddonId; ;
            model.Addon = _context.FieldMappings.Where(x => x.MapId == i).Select((x) => new EmailCreateViewModel.AddonSetup
            {
                AddonId = x.MapId,
                AddonCode = x.MapCode
            }).ToList();
            return model;
        }
        public EmailCreateViewModel ValidateEmailCode(string code)
        {
            var model = new EmailCreateViewModel();

            model.EmailTemplateView = _context.EmailTemplate.Where(x => x.Code == code).Select((x) => new EmailCreateViewModel.EmailTemplate
            {
                Code = x.Code,
            }).ToList();
            return model;
        }

        public EmailCreateViewModel Find_EmailTemplate(int id)
        {
            var model = new EmailCreateViewModel();

            model.EmailTemplateDetails = _context.EmailTemplate.AsEnumerable().Where(x => x.Id == id).Select((x) => new EmailCreateViewModel.EmailTemplate
            {
                Body = x.Body,
                CC = x.CC.Split(','),
               To = x.To.Split(','),
                QueryTo = x.QueryTo,
                Subject = x.Subject,
                Code = x.Code,
                CredCode = x.CredCode,
                Description = x.Description,
                FileCode = x.FileCode,
                Id = x.Id,
                QueryCC = x.QueryCC,
                IsActive = x.IsActive,
                QueryCode = x.QueryCode,
                Company = x.Company
            }).FirstOrDefault();


            model.QueryManagerView = _context.QueryManager.Where(x => x.IsActive == true).Select((x) => new EmailCreateViewModel.QueryManager
            {
                Id = x.Id,
                QueryCode = x.QueryCode
            }).ToList();

            model.EmailView = _context.EmailSetup.Select(x => new EmailCreateViewModel.Email
            {
                EmailId = x.EmailId,
                EmailCode = x.EmailCode
            }).ToList();

            model.DocumentList = _context.Documents.Select(x => new EmailCreateViewModel.Document
            {
                Code = x.Code,
                Id = x.Id,
                FileName = x.FileName,
                IsActive = x.IsActive
            }).ToList();

            model.CompanyList = _context.CompanyDetails.Select(x => new EmailCreateViewModel.Company
            {

                Id = x.Id,
                FileName = x.FileName,
                IsActive = x.IsActive,
                Address = x.Address,
                MobileNo = x.MobileNo,
                TelNo = x.TelNo,
                CompanyName = x.CompanyName,
                FilePath = x.FilePath

            }).ToList();
            return model;
        }

        public EmailCreateViewModel ListConnectionString()
        {
            var model = new EmailCreateViewModel();

            model.QueryManagerView = _context.QueryManager.Where(x => x.IsActive == true).Select((x) => new EmailCreateViewModel.QueryManager
            {
                Id = x.Id,
                QueryCode = x.QueryCode
            }).ToList();

            model.EmailView = _context.EmailSetup.Select(x => new EmailCreateViewModel.Email
            {
                EmailId = x.EmailId,
                EmailCode = x.EmailCode
            }).ToList();

            model.DocumentList = _context.Documents.Select(x => new EmailCreateViewModel.Document
            {
                Code = x.Code,
                Id = x.Id,
                FileName = x.FileName,
                IsActive = x.IsActive
            }).ToList();
            model.CompanyList = _context.CompanyDetails.Select(x => new EmailCreateViewModel.Company
            {

                Id = x.Id,
                FileName = x.FileName,
                IsActive = x.IsActive,
                Address = x.Address,
                MobileNo = x.MobileNo,
                TelNo = x.TelNo,
                CompanyName = x.CompanyName,
                FilePath = x.FilePath

            }).ToList();
            return model;

        }

        public EmailCreateViewModel Get_AddonSetup()
        {
            var addon = new EmailCreateViewModel();
            addon.Addon = _context.FieldMappings.Select(x => new EmailCreateViewModel.AddonSetup
            {
                AddonCode = x.MapCode
            }).ToList();
            return addon;
        }

        public int SaveChanges()
        {
            return _context.SaveChanges();
        }

        public bool Update_EmailSetup(EmailLogs eml, string code, string[] cc, int id)
        {
            var recipients = "";
            //foreach (var item in cc)
            //{
            //    recipients += item + ",";
            //}
            var Code = _context.FieldMappings.Where(x => x.MapCode == code).FirstOrDefault();
            var email = eml;
            email.EmailCc = eml.EmailCc;
            email.AddonId = Code.MapId;
            email.UpdateDate = DateTime.Now;
            email.UpdateUserID = id;
            email.IsActive = true;
            _context.Entry(email).State = System.Data.Entity.EntityState.Modified;
            _context.SaveChanges();
            SaveChanges();
            return true;
        }

        public EmailCreateViewModel View_EmailSetup()
        {

            EmailCreateViewModel model = new EmailCreateViewModel();
            model.EmailView = _context.Emails.Select(x =>
                    new EmailCreateViewModel.Email
                    {
                        EmailId = x.EmailId,
                        EmailCode = x.EmailCode,
                        EmailName = x.EmailName,
                        EmailFrom = x.EmailFrom,
                        EmailSubject = x.EmailSubject,
                        EmailHost = x.EmailHost,
                        EmailPort = x.EmailPort,
                        EmailSsl = x.EmailSsl,
                        EmailTo = x.EmailTo,
                        EmailCc = x.EmailCc,
                        EmailLog = x.EmailLog,
                        IsActive = x.IsActive,
                        AddonId = x.AddonId

                    }).ToList();

            model.Addon = _context.FieldMappings.Select(x => new EmailCreateViewModel.AddonSetup
            {
                AddonId = x.MapId,
                AddonCode = x.MapCode


            }).ToList();

            model.EmailTemplateView = _context.EmailTemplate.Select(x => new EmailCreateViewModel.EmailTemplate
            {
                Code = x.Code,
                Description = x.Description,
                Subject = x.Subject,
                Id = x.Id,
                IsActive = x.IsActive,
                Body = x.Body
            }).ToList();

            return model;
        }
    }
}