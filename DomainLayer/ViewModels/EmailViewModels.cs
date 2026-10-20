using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace DomainLayer.ViewModels
{
    public class EmailViewModel
    {
        public EmailCredentials EmailCreds { get; set; }
        public List<EmailCredentials> EmailCredsList { get; set; }
        public DataTable QueryTable { get; set; }
        public DataTable ToTable { get; set; }
        public DataTable CcTable { get; set; }
        public DbConnection DatabaseConnectionView { get; set; }

        public class DbConnection
        {
            public int Id { get; set; }
            public string QueryCode { get; set; }
            public string QueryName { get; set; }
            public string ConnectionString { get; set; }
            public string ConnectionType { get; set; }
            public string QueryString { get; set; }
            public bool IsActive { get; set; }
        }
        public class EmailCredentials
        {
            public string EmailDesc { get; set; }
            public string EmailCredCode { get; set; }
            public string SchedCode { get; set; }
            public string EmailFrom { get; set; }
            public string EmailPassword { get; set; }
            public string EmailTo { get; set; }
            public string EmailCc { get; set; }
            public string EmailSubject { get; set; }
            public string EmailHost { get; set; }
            public string AddonDBName { get; set; }
            public string AddonIPAddress { get; set; }
            public string AddonServerName { get; set; }
            public string AddonDBuser { get; set; }
            public string AddonDBPassword { get; set; }
            [AllowHtml]
            public string Body { get; set; }
            public int EmailPort { get; set; }
            public string ConnectionType { get; set; }
            public string ConnectionId { get; set; }
            public string Query { get; set; }
            public string ToQuery { get; set; }
            public string CcQuery { get; set; }
            public string FileName { get; set; }
            public string FilePath { get; set; }
            public string SavePath { get; set; }
            public string FileCred { get; set; }
            public string QueryData { get; set; }
            public string Company { get; set; }
        }

        public QuerySetup QueryDetails { get; set; }
        public class QuerySetup
        {
            public int Id { get; set; }
            public string QueryCode { get; set; }
            public string QueryString { get; set; }
            public string ConnectionType { get; set; }
            public string ConnectionString { get; set; }

        }

        public FileCredential FileCredentials { get; set; }
        public class FileCredential
        {
            public string DbName { get; set; }
            public string IpAddress { get; set; }
            public string ServerName { get; set; }
            public string SapUser { get; set; }
            public string SapPassword { get; set; }
        }

        public Company CompanyDetails { get; set; }
        public class Company
        {
            public string CompanyName { get; set; }
            public string FilePath { get; set; }
            public string FileName { get; set; }
            public string Address { get; set; }
            public string MobileNo { get; set; }
            public string TelNo { get; set; }
        }
    }
}
