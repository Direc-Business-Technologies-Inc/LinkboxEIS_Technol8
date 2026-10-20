using DomainLayer.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Web.Mvc;

namespace DomainLayer.ViewModels
{

    public class SyncViewModel
    {

        public List<Sync> SyncView { get; set; }
        public class Sync
        {
            public int Id { get; set; }
            public string Code { get; set; }
            public string FileType { get; set; }
            public string IpAddress { get; set; }
            public string Path { get; set; }
            public string FtpPath { get; set; }
            public string FtpUser { get; set; }
            public string FtpPass { get; set; }
            public string DbName { get; set; }
            public string DbUser { get; set; }
            public string DbPass { get; set; }
            public int ServerPort {  get; set; }
            public string DbVersion { get; set; }
            public bool IsActive { get; set; }
        }
        public List<SAPVersion> SAPList { get; set; }
        public class SAPVersion
        {
            public int Id { get; set; }
            public string SAPDBVersion { get; set; }
        }


        public List<CrystalExtract> CrystalExtractView { get; set; }
        public class CrystalExtract
        { 
            public int Id { get; set; }

            public string Name {get; set; }

            public int QueryId { get; set; }
            public string QueryCode { get; set; }
            public string QueryString { get; set; }

            public int SyncId { get; set; }
            public int DocumentId { get; set; }

            public string DocumentName { get; set; }

            public string DocumentCode { get; set; }

            public int APIId { get; set; } 

            public string APICode { get; set; }
            public bool IsActive { get; set; }

        }

        public List<SyncQueryMap> SyncQueryMapsView { get; set; }
        public class SyncQueryMap
        {
            public int ReportID { get; set; }
            public int QueryID { get; set; }
            public string QueryCode { get; set; }
            public int VisOrder { get; set; }
        }
        public List<ReportExtract> ReportExtractView { get; set; }
        public class ReportExtract
        {
            public int Id { get; set; }

            public string Name { get; set; }

            public int QueryId { get; set; }
            public string QueryCode { get; set; }
            public string QueryString { get; set; }

            public int DocumentId { get; set; }

            public string DocumentName { get; set; }

            public string DocumentCode { get; set; }

            public int APIId { get; set; }

            public string APICode { get; set; }
            public bool IsActive { get; set; }

        }


        public List<SyncQuery> SyncQueryView { get; set; }
        public class SyncQuery
        {
            public int Id { get; set; }
            public string SyncQueryCode { get; set; }
            public bool IsActive { get; set; }

            public int QueryId { get; set; }
            public string QueryCode { get; set; }
            public string QueryString { get; set; }

            public int SyncId { get; set; }
            public string SyncCode { get; set; }
            public string Path { get; set; }
            public string RemotePath { get; set; }
            public string RemoteUser { get; set; }
            public string RemotePassword { get; set; }
            public string DbName { get; set; }
            public string DbUser { get; set; }
            public string DbPass { get; set; }
            public string DbVersion { get; set; }
            public string FileType { get; set; }
            public string IpAddress { get; set; }
        }

        public List<Query> QueryView { get; set; }
  
        public List<Query> ReportQueryView { get; set; }
        public class Query
        {
            public int Id { get; set; } 
            public string Code { get; set; }
            public string QueryString { get; set; }
            public bool IsActive { get; set; }
        }
        public List<DocQuery> DocumentView { get; set; }

        public class DocQuery
        {
            public int Id { get; set; }
            public string Code { get; set; }
            public string FileName { get; set; }

            
            public bool IsActive { get; set; }
        }

        public List<PathQuery> ReportSyncView { get; set; }

        public class PathQuery
        {
            public int Id { get; set; }
            public string Code { get; set; }
            


            public bool IsActive { get; set; }
        }

        public List<APISetup> APIView { get; set; }

        public class APISetup
        {
            public int Id { get; set; }
            public string APICode { get; set; }
   
            public bool IsActive { get; set; }
        }
    }
    public class DepositCreateViewModel
    {
        public List<Deposit> DepositView { get; set; }
        public class Deposit
        {
            public int BrkId { get; set; }
            public string BrkCode { get; set; }
            public string BrkDescription { get; set; }
            public bool IsActive { get; set; }

        }
    }

    public class EmailCreateViewModel
    {
        public List<Email> EmailView { get; set; }
        public class Email
        {
            [Key]
            public int EmailId { get; set; }

            [Required]
            public string EmailCode { get; set; }
            [Required]
            public string EmailName { get; set; }
            [Required]
            [EmailAddress(ErrorMessage = "Invalid Email Address.")]
            [DataType(DataType.EmailAddress)]
            public string EmailFrom { get; set; }
            [Required]
            [DataType(DataType.Password)]
            public string EmailPassword { get; set; }

            public string EmailSubject { get; set; }
            [Required]
            public string EmailHost { get; set; }
            [Required]
            public int EmailPort { get; set; }
            [Required]
            public string EmailSsl { get; set; }


            public enum Emaillog { Log, Transaction }

            [EmailAddress(ErrorMessage = "Invalid Email Address.")]
            [Required]
            [DataType(DataType.EmailAddress)]
            public string EmailTo { get; set; }

            [EmailAddress(ErrorMessage = "Invalid Email Address.")]
            [DataType(DataType.EmailAddress)]
            public string EmailCc { get; set; }
            [Required]
            public string EmailLog { get; set; }

            public bool IsActive { get; set; }

            public int AddonId { get; set; }
            public string[] ECc { get; set; }
        }

        public List<AddonSetup> Addon { get; set; }
        public class AddonSetup
        {
            public int AddonId { get; set; }
            public string AddonCode { get; set; }
        }


        public List<EmailSslDetails> EmailSsls { get; set; }
        public class EmailSslDetails
        {
            public string EmailSsl { get; set; }
        }


        public List<EmailTemplate> EmailTemplateView { get; set; }
        public EmailTemplate EmailTemplateDetails { get; set; }
        public class EmailTemplate
        {
            public int Id { get; set; }
            public string Code { get; set; }
            public string Description { get; set; }
            public string CredCode { get; set; }
            public string QueryTo { get; set; }
            public string QueryCode { get; set; }
            public string QueryCC { get; set; }
            public string Subject { get; set; }
            [AllowHtml]
            public string Body { get; set; }
            public bool IsActive { get; set; }
            public string[] To { get; set; }
            public string[] CC { get; set; }
            public string FileCode { get; set; }
            public string Company { get; set; }

        }

        public List<QueryManager> QueryManagerView { get; set; }
        public class QueryManager
        {
            public int Id { get; set; }
            public string QueryCode { get; set; }
            public string QueryName { get; set; }
            public string ConnectionType { get; set; }
            public string ConnectionString { get; set; }
            public string QueryString { get; set; }
            public bool IsActive { get; set; }
            public DateTime CreateDate { get; set; }
            public string CreateUserId { get; set; }
            public DateTime UpdateDate { get; set; }
            public string UpdateUserId { get; set; }

        }

        public List<Document> DocumentList { get; set; }
        public Document DocumentDetails { get; set; }
        public class Document
        {
            public int Id { get; set; }
            public string Code { get; set; }
            public string FileName { get; set; }
            public string SavePath { get; set; }
            public bool IsActive { get; set; }
        }

        public List<Company> CompanyList { get; set; }
        public Company CompanyDetails { get; set; }
        public class Company
        {
            public int Id { get; set; }
            public string CompanyName { get; set; }
            public string FileName { get; set; }
            public string FilePath { get; set; }
            public string Address { get; set; }
            public string MobileNo { get; set; }
            public string TelNo { get; set; }
            public bool IsActive { get; set; }
        }
    }

    public class EmailTemplateCreateViewModel
    {
        public Email EmailContent { get; set; }
        public List<UserAccountEmail> AccountEmailList { get; set; }
        public List<EmailAddress> EmailAddressList { get; set; }
        public class Email
        {
            public int Id { get; set; }
            public string Code { get; set; }
            public string Description { get; set; }
            public string Subject { get; set; }
            public string Body { get; set; }
            public bool IsActive { get; set; }
            public string[] SelectedEmailAddressTo { get; set; }
            public string[] SelectedEmailAddressCC { get; set; }

        }
        public class UserAccountEmail
        {
            public int Id { get; set; }
            public string EmailType { get; set; }
            public string EmailAddress { get; set; }
            public bool IsActive { get; set; }
            public int UserAccountId { get; set; }
        }
        public class EmailAddress
        {
            public string Email { get; set; }
        }
    }




    public class SchedCreateViewMdel
    {
        public List<Schedule> SchedView { get; set; }
        public class Schedule
        {
            [Key]
            public int SchedId { get; set; }

            public string SchedCode { get; set; }

            public string Frequency { get; set; }

            public DateTime StartDate { get; set; }
            public string StartDateString { get { return this.StartDate.ToString("yyyy-MM-dd"); } }
            public DateTime StartTime { get; set; }
            public string StartTimeString { get { return this.StartTime.ToString("HH:mm"); } }
            public string Process { get; set; }
            public string Api { get; set; }
            public string ScheduleType { get; set; }
            public string Credential { get; set; }
            public bool IsActive { get; set; }
        }
        public List<API> APIList { get; set; }
        public class API
        {
            public int APIId { get; set; }
            public string APICode { get; set; }
        }
        public List<process> ProcessList { get; set; }
        public class process
        {
            public int ProcessId { get; set; }
            public string ProcessCode { get; set; }
            public string ProcessName { get; set; }
            public string ProcessType { get; set; }


        }

        public List<crystal> CrystalList { get; set; }
        public class crystal
        {
            public int CrystalId { get; set; }
            public string CrystalCode { get; set; }
          
        }

        public List<Email> EmailList { get; set; }
        public List<Email> EmailCred { get; set; }
        public class Email
        {
            public int EmailId { get; set; }
            public string EmailCode { get; set; }
            public string EmailName { get; set; }
        }

        public List<Sync> SyncList { get; set; }
        public class Sync
        {
            public int SyncQueryId { get; set; }
            public string SyncQueryCode { get; set; }
        }



    }

    public class ProcessCreateViewModel
    {
        public List<process> ProcessView { get; set; }
        public class process
        {
            [Key]
            public int ProcessId { get; set; }
            public int MapId { get; set; }
            public string ProcessCode { get; set; }
            public string ProcessName { get; set; }
            public bool PostSAP { get; set; }
            public string FieldMappingCode { get; set; }
            public string FieldMappingName { get; set; }
            public string ModName { get; set; }
            public bool IsActive { get; set; }
            public string APICode { get; set; }
            public string SAPCode { get; set; }
            public string ProcessType { get; set; }
        }
        

        public List<mapping> MapList { get; set; }

        public class mapping
        {
            public int MapId { get; set; }
            public string MapCode { get; set; }

            public string MapName { get; set; }

            public string AddonCode { get; set; }
        }
        public List<opsmapping> opsMapList { get; set; }

        public class opsmapping
        {
            public int MapId { get; set; }
            public string MapCode { get; set; }

            public string MapName { get; set; }


        }


        public List<Email> EmailList { get; set; }
        public class Email
        {
            public int EmailId { get; set; }
            public string EmailCode { get; set; }
        }

        public List<API> APIList { get; set; }
        public class API
        {
            public int APIId { get; set; }
            public string APICode { get; set; }
        }
        public List<SapSetup> SapList {get;set;}
        public class SapSetup
        {
            public int SAPId { get; set; }
            public string SAPCode { get; set; }            
        }
    }




    public class QueryManagerViewModel
    {
        public List<QueryManager> QueryManagerView { get; set; } 
        public class QueryManager
        {
            public int Id { get; set; }
            public string QueryCode { get; set; }
            public string QueryName { get; set; }
            public string ConnectionType { get; set; }
            public string ConnectionString { get; set; }
            public string QueryString { get; set; }
            public bool IsActive { get; set; }
            public DateTime CreateDate { get; set; }
            public string CreateUserId { get; set; }
            public DateTime UpdateDate { get; set; }
            public string UpdateUserId { get; set; }

        }
    }

    public class QueryManagerMapViewModel
    {
        public List<QueryManagerMap> QueryManagerMapView { get; set; }
        public class QueryManagerMap
        {
            public int Id { get; set; }
            public string Field { get; set; }
            public string Condition { get; set; }
            public string Value { get; set; }
            public int QueryId { get; set; }
          
            public string DataType { get; set; }
            public bool IsActive { get; set; }
            public DateTime CreateDate { get; set; }
            public string CreateUserId { get; set; }
            public DateTime UpdateDate { get; set; }
            public string UpdateUserId { get; set; }

        }
    }
}