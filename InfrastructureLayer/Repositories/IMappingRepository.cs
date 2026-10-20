using DomainLayer;
using DomainLayer.ViewModels;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InfrastructureLayer.Repositories
{
    public interface IMappingRepository
    {
        MapCreateViewModel View_Mapping();
        MapCreateViewModel View_Query();
        MapCreateViewModel View_API();
       MapCreateViewModel Find_Map(int id);
        MapCreateViewModel APItoAPIFind_Map(int id);
        MapCreateViewModel DataFileToSAP(int id);


        MapCreateViewModel opsFind_Map(int id);
        MapCreateViewModel Validate_MapCode(string code);
        MapCreateViewModel OPSFind_File(int id);
        bool AddTable_OpsMapId(int id,string TableMap);
        MapCreateViewModel GetFieldMap(int id, string tablename);

        MapCreateViewModel ValidateMap(string code);
        MapCreateViewModel ValidateMapModule(string SAPCode, string Module);
        SetupCreateViewModel.AddonViewModel Select_AddonSetup(string code);
        void Create_FieldMapping(FieldMapping field, int id);
        void Update_FieldMapping(FieldMapping field, int id);
        void Create_Headers(List<string[]> headers, int id, string table, int check, int newid, int mapid);

        void Create_SourceDestinationTable(List<string[]> sourcefieldtb, List<string[]> headerval, int id, string table, int check, int newid, int mapid);


        //void Create_QueryMap(List<string[]> headers, int id, string table, int check, int newid, int mapid);
        void Create_Rows(List<string[]> rows, int id, string table, int check, int newid, int mapid);
        void CreateParameters(List<string[]> Parameters, int mapid, string apicode);
        List<string[]> NewFieldValue(List<string[]> list, int count);

        string Get_Constring(SetupCreateViewModel.AddonViewModel AddonPath);
        void GenerateTable(string database, string constring, List<string[]> headerfield, List<string[]> rowfield,
                                  string rowrname, string headername, int headercount, int rowcount);
        void GenerateRowTable(string database, string constring, List<string[]> rowfield, List<DataRow> TableList,
                          string rowrname, int rowcount, List<DataRow> RowsPrimaryKey);
        void GenerateHeaderTable(string database, string constring, List<string[]> headerfield, string headername, int headercount, List<DataRow> HeaderPrimaryKey);
        MapCreateViewModel Populate(string table, string code, string header, string row);
        MapCreateViewModel PopulateAPI(string table, string code, string header, string row, string APICode, int MapId);
        MapCreateViewModel PopulateFileAPI(string FileName, string code, string header, string row, string APICode);
        MapCreateViewModel PopulateSAPAPI(string SourceAPICode, string HeaderTable, string RowTable, string DestinationAPICode, string Module);
        JObject FetchSAPAPIData(string Code, string Module);
        List<MapCreateViewModel.Data> FetchSAPDataTypes();
        MapCreateViewModel Get_DataType(string table, string code, string field);
        SetupCreateViewModel.SAPViewModel Select_SAPSetup(string code);
        int SaveChanges();
    }
}
