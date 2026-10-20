using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace DomainLayer.ViewModels
{


    public class SetupCreateViewModel
    {
      
        public List<DBViewModel> DBView { get; set; }
        public class DBViewModel
        {
            public int ConfigId { get; set; }

            public string SAPCode { get; set; }

            public string SAPServer { get; set; }

            public string SAPDBName { get; set; }

            public string AddonCode { get; set; }

            public string AddonServer { get; set; }

            public string AddonDB { get; set; }

            public string PathCode { get; set; }

            public string Path { get; set; }

            public string PathServer { get; set; }

            public bool IsActive { get; set; }
        }

        public List<AddonViewModel> AddonView { get; set; }

        public class AddonViewModel
        {
            public int AddonId { get; set; }
            public string AddonCode { get; set; }

            public string AddonDBVersion { get; set; }

            public string AddonServerName { get; set; }

            public string AddonIPAddress { get; set; }

            public string AddonDBName { get; set; }

            public int? AddonPort { get; set; }

            public string AddonDBuser { get; set; }

            public string AddonDBPassword { get; set; }

            public bool IsActive { get; set; }
        }

        public List<DbConnection> DatabaseConnectionView { get; set; }

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

     
        public SAPViewModel SAPDbDetails { get; set; }
        public List<SAPViewModel> SAPView { get; set; }

        public class SAPViewModel
        {
            public int SAPId { get; set; }
            public string SAPCode { get; set; }

            public string SAPDBVersion { get; set; }
            public int SAPLicensePort { get; set; }
            public string SAPSldServer { get; set; }
            public string SAPServerName { get; set; }

            public string SAPIPAddress { get; set; }

            public string SAPDBName { get; set; }

            public string SAPVersion { get; set; }

            public int SAPDBPort { get; set; }

            public string SAPDBuser { get; set; }

            public string SAPDBPassword { get; set; }

            public string SAPUser { get; set; }

            public string SAPPassword { get; set; }
            public bool IsActive { get; set; }
        }
        public ApiDescription APIDesc { get; set; }
        public List<ApiDescription> ApiDescriptions { get; set; }
        public class ApiDescription
        {
            public string APIUrl { get; set; }
            public string Method { get; set; }
        }

        public APIViewModel APIDetails { get; set; }
        public List<APIViewModel> APIView { get; set; }

        public class APIViewModel
        {
            public int APIId { get; set; }
            public string APICode { get; set; }

            public string APIMethod { get; set; }
            public string APIModule { get; set; }
            public string APIURL { get; set; }
            public bool IsActive { get; set; }
            public string APIKey { get; set; }
            public string APISecretKey { get; set; }
            public string APIToken { get; set; }
            public string APILoginUrl { get; set; }
            public string APILoginBody { get; set; }

            public string Platform { get; set; }
            public string AuthCode { get; set; }
            public string AccessToken { get; set; }
            public string RefreshToken { get; set; }
            public string ShopCode { get; set; }
            public string AccessTokenExpiry { get; set; }
            public string RefreshTokenExpiry { get; set; }

        }

        public List<EmailViewModel> EmailView { get; set; }

        public class EmailViewModel
        {
            public int EmailId { get; set; }
            public string EmailCode { get; set; }
            public string EmailDesc { get; set; }
            public string Email { get; set; }
            public string Password { get; set; }
            public string SMTPClient { get; set; }
            public int Port { get; set; }
            public string DisplayName { get; set; }
            public bool IsActive { get; set; }
            public DateTime CreateDate { get; set; }
            public int CreateUserID { get; set; }
        }

        public List<Files> FileList { get; set; }
        public class Files
        {
            public string FileName { get; set; }
            public string FilePath { get; set; }
        }

        public List<QueryManagerDocView> QueryManagerDocViewList { get; set; }
        public class QueryManagerDocView
        {
            public int QueryId { get; set; }
            public string QueryCode { get; set; }
            
            public string QueryString { get; set; }

            public bool IsActive { get; set; }

        }
        public List<Dictionary<string, object>> DocumentMapDict { get; set; }
        public List<string> Columns { get; set; }
        public List<DocumentMapView> DocumentMapViewList { get; set; }
        public class DocumentMapView
        {
            public int id { get; set; }
            public string CrystalParam { get; set; }

            public string QueryField { get; set; }

            public int DocId { get; set; }

        }




        public List<PathViewModel> PathView { get; set; }
        public class PathViewModel
        {
            public int PathId { get; set; }
            public string PathCode { get; set; }

            public string LocalPath { get; set; }
            public string BackupPath { get; set; }

            public string ErrorPath { get; set; }

            public string RemotePath { get; set; }

            public string RemoteServerName { get; set; }

            public string RemoteIPAddress { get; set; }

            public int? RemotePort { get; set; }

            public string RemoteUserName { get; set; }

            public string RemotePassword { get; set; }

            //public string FileType { get; set; }

            public bool IsActive { get; set; }
        }
        public List<SAPVersion> SAPList { get; set; }
        public class SAPVersion
        {
            public int Id { get; set; }
            public string SAPDBVersion { get; set; }
        }

        public List<Parameter> ParameterList { get; set; }
        public Parameter ParameterDetails { get; set; }
        public class Parameter
        {
            public int Id { get; set; }
            public string Code { get; set; }
            public string ParameterType { get; set; }
            public int Value { get; set; }
            public bool IsActive { get; set; }
        }

        public List<Document> DocumentList { get; set; }
        public Document DocumentDetails { get; set; }
        public class Document
        {
            public int Id { get; set; }
            public string Code { get; set; }
            public string FileName { get; set; }
            public string Credential { get; set; }
            public string SavePath { get; set; }
            public bool IsActive { get; set; }
            public int QueryManagerID { get; set; }
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

        public List<APIManager> APIManagerList { get; set; }
        public class APIManager
        {
            public int Id { get; set; }
            public string Title { get; set; }
            public string APICode { get; set; }
            public string Method { get; set; }
            public string QueryCode { get; set; }
            public string QueryName { get; set; }
            public bool IsActive { get; set; }
            public DateTime CreateDate { get; set; }
            public int CreateUserId { get; set; }
            public DateTime? UpdateDate { get; set; }
            public int? UpdateUserId { get; set; }

        }

        public class APIQuerySetup
        {
            public int Id { get; set; }
            public int QueryId { get; set; }
            public string QueryCode { get; set; }
            public string QueryString { get; set; }
            public string SAPServerName { get; set; }
            public string SAPIPAddress { get; set; }
            public string SAPDBName { get; set; }
            public int SAPLicensePort { get; set; }
            public int SAPDBPort { get; set; }
            public string SAPDBuser { get; set; }
            public string SAPDBPassword { get; set; }
            public string SAPDBVersion { get; set; }
            public string SAPUser { get; set; }
            public string SAPPassword { get; set; }
        }

        public ModuleSetupViewModel ModuleSetup { get; set; }
        public List<ModuleSetupViewModel> ModuleSetups { get; set; }
        public class ModuleSetupViewModel
        {
            public int Id { get; set; }
            public string ModuleCode { get; set; }
            public string ModuleName { get; set; }
            public string PrimaryKey { get; set; }
            public bool IsActive { get; set; }
            public string EntityType { get; set; }
            public string EntityName { get; set; }
        }

    }

    public class ModuleCreateViewModel
    {
        public List<Module> ModuleView { get; set; }
        public class Module
        {
            [Key]
            public int ModId { get; set; }

            public string ModuleCode { get; set; }

            public string ModName { get; set; }

            public bool IsActive { get; set; }
        }
    }


    public class MapCreateViewModel
    {
        public List<ClassificationFile> ClassificationFiles { get; set; } = new List<ClassificationFile>();

        public class ClassificationFile
        {
            public string FileName { get; set; }     // option value
            public string DisplayName { get; set; }  // option text
        }
        public List<DocumentMapView> DocumentMapViewList { get; set; }
        public class DocumentMapView
        {
            public int id { get; set; }
            public string CrystalParam { get; set; }

            public string QueryField { get; set; }

            public int DocId { get; set; }
            public int CreateUserId { get; set; }

        }
        public List<FieldMapping> PlatformFieldView { get; set; }

        public FieldMapping FieldMapDetails { get; set; }
        public List<FieldMapping> FieldView { get; set; }
        public class FieldMapping
        {
            [Key]
            public int MapId { get; set; }

            public string MapCode { get; set; }
            public string QueryCode {  get; set; }
            public string ConnectionString { get; set; }
            public string QueryString { get; set; }

            public string MapName { get; set; }
            public int PathId { get; set; }

            public string PathCode { get; set; }

            public string HeaderWorksheet { get; set; }
            public string RowWorksheet { get; set; }

            public string FileName { get; set; }

            public string FileType { get; set; }

            public string SAPCode { get; set; }

            public string AddonCode { get; set; }

            public string ModuleName { get; set; }
            public string DataType { get; set; }
            public string APICode { get; set; }
            public string DestModule { get; set; }
            public string Localpath { get; set; }
            public string Platform { get; set; }
        }
        public OPSFieldTable OPSFieldTableDetails { get; set; }
        public List<OPSFieldTable> OPSTableView { get; set; }
        public class OPSFieldTable
        { 
            public int SAPTableId { get; set; }
            public int MapId { get; set; }
            public string SourceTableName { get; set; }
            public string SourceColumnName { get; set; }

            public string SourceRowData { get; set; }

            public string Pathcode { get; set; }

            public string FileType { get; set; }

            public string FileName { get; set; }    

            public string SAPTableNameModule {get; set;}

            public int PathId { get; set; }


        }
            public OPSFieldMapping OPSFieldMapDetails { get; set; }
            public List<OPSFieldMapping> OPSFieldView { get; set; }
            public class OPSFieldMapping
            {
                [Key]
                public int MapId { get; set; }

                public string MapCode { get; set; }

                public string MapName { get; set; }

                public string APICode { get; set; }

                public string SAPCode { get; set; }
                public string FileName { get; set; }
                public string FileType { get; set; }

                public string DestModule { get; set; }


            //public string SAPCode { get; set; }
            public string ModuleName { get; set; }
            //public int PathId { get; set; }
            //public string PathCode { get; set; }


        }


        public List<OPSFileMapping> OPSFileView { get; set; }
        public class OPSFileMapping
        {
            public string FileName { get; set; }
            public string FileType { get; set; }
            
            public string SAPTableName { get; set; }

        }

            public ModuleSetupViewModel ModuleSetup { get; set; }
        public List<ModuleSetupViewModel> ModuleSetups { get; set; }
        public class ModuleSetupViewModel
        {
            public int Id { get; set; }
            public string ModuleCode { get; set; }
            public string ModuleName { get; set; }
            public string PrimaryKey { get; set; }
            public bool IsActive { get; set; }
        }

        public List<SAPCodes> SAPCodeList { get; set; }
        public class SAPCodes
        {
            public int SAPId { get; set; }
            public string SAPCode { get; set; }

            public string SAPDBVersion { get; set; }
            public int SAPLicensePort { get; set; }

            public string SAPServerName { get; set; }

            public string SAPIPAddress { get; set; }

            public string SAPDBName { get; set; }

            public string SAPVersion { get; set; }

            public int SAPDBPort { get; set; }

            public string SAPDBuser { get; set; }

            public string SAPDBPassword { get; set; }

            public string SAPUser { get; set; }

            public string SAPPassword { get; set; }
        }

        public List<AddonCodes> AddonCodeList { get; set; }
        public class AddonCodes
        {
            public int AddonId { get; set; }
            public string AddonCode { get; set; }

            public string AddonDBVersion { get; set; }

            public string AddonServerName { get; set; }

            public string AddonIPAddress { get; set; }

            public string AddonDBName { get; set; }

            public int? AddonPort { get; set; }

            public string AddonDBuser { get; set; }

            public string AddonDBPassword { get; set; }
        }

        public List<PathCodes> PathCodeList { get; set; }
        public class PathCodes
        {
            public int PathId { get; set; }
            public string PathCode { get; set; }

            public string LocalPath { get; set; }
            public string BackupPath { get; set; }
            public string ErrorPath { get; set; }

            public string RemotePath { get; set; }

            public string RemoteServerName { get; set; }

            public string RemoteIPAddress { get; set; }

            public int? RemotePort { get; set; }

            public string RemoteUser { get; set; }

            public string RemotePassword { get; set; }

            //public string FileType { get; set; }
        }


        public List<Header> Headers { get; set; }
        public class Header
        {
            //[Key]
            //public int HeaderId { get; set; }
            [Key]
            public int MapId { get; set; }
            [Key]
            public string SourceFieldId { get; set; }
            public string TableName { get; set; }            
            public string DestinationField { get; set; }
            public string DataType { get; set; }
            public string Length { get; set; }
            public bool IsRequired { get; set; }
            public string DefaultValue { get; set; }
            public string IsNullable { get; set; }
            public int VisOrder { get; set; }

            public string SourceType { get; set; }
            public string DestinationTableName { get; set; }
            public string SourceHeaderStart { get; set; }
            public string SourceRowStart { get; set; }
            public string ConditionalQuery { get; set; }
        }

        public List<OPSFieldSet> FieldSetView { get; set; }
        public class OPSFieldSet
        {
            //[Key]
            //public int HeaderId { get; set; }
 
            public int MapId { get; set; }

            public int SAPTableId { get; set; }

            public string SourceField { get; set; }
   
            public string DestinationField { get; set; }

            public string DataType { get; set; }
            public string Length { get; set; }
            public bool IsRequired { get; set; }

            public string ConditionalQuery { get; set; }

            public int VisOrder { get; set; }

            public string SAPTableNameModule { get; set; }

            //public string SourceType { get; set; }
            //public string DestinationTableName { get; set; }
            //public string SourceHeaderStart { get; set; }
            //public string SourceRowStart { get; set; }
           
        }

        public List<Row> Rows { get; set; }
        public class Row
        {
            //[Key]
            //public int RowId { get; set; }
            [Key]
            public int MapId { get; set; }
            [Key]
            public string SAPRowFieldId { get; set; }
            public string TableName { get; set; }
            public string AddonRowField { get; set; }
            public string DataType { get; set; }
            public string Length { get; set; }
            public bool IsRequired { get; set; }
            public string DefaultValue { get; set; }
        }
        public List<HeaderHanaField> HeaderHanaFields { get; set; }
        public List<HeaderHanaField> HeaderAPIFields { get; set; }
        public class HeaderHanaField
        {
            [Key]
            public int Id { get; set; }
            public string TableName { get; set; }
            public string ColumnName { get; set; }
        }
        public List<RowHanaField> RowHanaFields { get; set; }
        public List<RowHanaField> RowAPIFields { get; set; }
        public class RowHanaField
        {
            [Key]
            public int Id { get; set; }
            public string TableName { get; set; }
            public string ColumnName { get; set; }
        }

        public List<Data> DataTypes { get; set; }
        public class Data
        {
            [Key]
            public int Id { get; set; }
            public string DataType { get; set; }

        }


        public List<HeadData> HeaderDataTypes { get; set; }
        public class HeadData
        {
            [Key]
            public int Id { get; set; }
            public string DataType { get; set; }
            public string Length { get; set; }
            public string Scale { get; set; }
            public string DBType { get; set; }
        }

        public List<RowData> RowDataTypes { get; set; }
        public class RowData
        {
            [Key]
            public int Id { get; set; }
            public string DataType { get; set; }
            public string Length { get; set; }
            public string Scale { get; set; }
            public string DBType { get; set; }
        }

        public List<QueryManager> QueryManagerView { get; set; }
        //public QueryManager MyQueryManager { get; set; }
        public class QueryManager
        {
            public int Id { get; set; }
            public string QueryCode { get; set; }
            public string QueryName { get; set; }
            public string QueryString { get; set; }
            public string ConnectionType { get; set; }
            public string ConnectionString { get; set; }
            public bool IsActive { get; set; }
            public DateTime CreateDate { get; set; }
            public int CreateUserId { get; set; }
            public DateTime? UpdateDate { get; set; }
            public int UpdateUserId { get; set; }

        }

        public List<QueryManagerMap> QueryManagerMapView { get; set; }
        //public QueryManager MyQueryManager { get; set; }
        public class QueryManagerMap
        {
            public int Id { get; set; }
            public string Field { get; set; }
            public string QueryId { get; set; }
            public string Value { get; set; }

            public string DataType { get; set; }


            public bool IsActive { get; set; }
            public DateTime CreateDate { get; set; }
            public int CreateUserId { get; set; }
            public DateTime? UpdateDate { get; set; }
            public int UpdateUserId { get; set; }

        }
        public List<APIViewModel> APIView { get; set; }

        public class APIViewModel
        {
            public int APIId { get; set; }
            public string APICode { get; set; }
            public string APIPlatform { get; set; }
            public string APIMethod { get; set; }
            public string APIURL { get; set; }
            public bool IsActive { get; set; }
            public string APIKey { get; set; }
            public string APISecretKey { get; set; }
            public string APIToken { get; set; }
        }
        public List<APIManager> APIManagerView { get; set; }
        public class APIManager
        {
            public int Id { get; set; }
            public string Title { get; set; }
            public string APICode { get; set; }
            public string Method { get; set; }
            public string QueryCode { get; set; }
            public string QueryName { get; set; }
            public string SAPCode { get; set; }
            public bool IsActive { get; set; }
            public DateTime CreateDate { get; set; }
            public int CreateUserId { get; set; }
            public DateTime? UpdateDate { get; set; }
            public int? UpdateUserId { get; set; }

        }

        public List<APIParameter> ApiParameters { get; set; }
        public class APIParameter
        {
            public string APICode { get; set; }
            public string APIParameterKey { get; set; }
            public string APIParameterValue { get; set; }
        }

        //public List<MapCreateViewModel> QueryManagerView { get; set; }
        //public class MapCreateViewModel
        //{
        //    public string APICode { get; set; }
        //    public string APIParameterKey { get; set; }
        //    public string APIParameterValue { get; set; }
        //}




    }
}