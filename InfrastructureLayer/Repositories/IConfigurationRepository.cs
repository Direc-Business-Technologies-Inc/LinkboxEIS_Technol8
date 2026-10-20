using DomainLayer;
using DomainLayer.Models;
using DomainLayer.ViewModels;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;

namespace InfrastructureLayer.Repositories
{
    public interface IConfigurationRepository
    {
        SetupCreateViewModel View_Setup(HttpConfiguration config);
        void Create_AddonSetup(AddonSetup addon, int id);
        SetupCreateViewModel Find_Addon(int id);
        SetupCreateViewModel Find_ConnectionString(string Type);
        SetupCreateViewModel GetConnection(string Type, string ConString);
        DataTable Fill_DataTable(SetupCreateViewModel model, string Query,int id);
        SetupCreateViewModel ValidateCode(string code);
       
        SetupCreateViewModel GetMethod(HttpConfiguration config, string APIUrl);
        bool Update_AddonSetup(AddonSetup addon, int id);
        void Create_SapSetup(SAPSetup addon, int id);
        void Create_Query(QueryManager addon, int id);
        void Create_QueryMap(QueryManagerMap map,List<string[]> headerval, int id,int newid);

        void Create_DocMap(DocumentMap map,List<string[]> storeval,int id,int doc_id);
        void Create_API(APIManager addon, int id);
        bool Update_APIManager(APIManager addon, int id);
        SetupCreateViewModel Find_APIManager(int id);
        SetupCreateViewModel Find_SAP(int id);
        bool Update_SapSetup(SAPSetup addon, int id);
        void Create_PathSetup(PathSetup path, int id);
        SetupCreateViewModel Find_Path(int id);
        SetupCreateViewModel Find_File(int id);

        SetupCreateViewModel GetCrystalParam(int id);

        List<string> FindQueryString(int id);
        SetupCreateViewModel FindCompany(int id);
        SetupCreateViewModel FindModule(int id);
        bool Update_PathSetup(PathSetup path, int id);
        SetupCreateViewModel Find_Addon(string code);
        SetupCreateViewModel Find_SAP(string code);
        SetupCreateViewModel Find_Path(string code);
        void Create_APISetup(APISetup addon, int id);
        void Create_EmailSetup(EmailSetup addon, int id);
        SetupCreateViewModel Find_API(int id);
        SetupCreateViewModel Find_Email(int id);
        SetupCreateViewModel Find_Query(int id);

        QueryManagerViewModel ValidateQueryManager(string code);
        QueryManagerMapViewModel Find_QueryMap(int id);
        MapCreateViewModel PopulateSAPDBTable(string Schema, string Module, SetupCreateViewModel.SAPViewModel model);
        MapCreateViewModel PopulateSAPDBTableRow(string Schema, string Module, SetupCreateViewModel.SAPViewModel model);
        MapCreateViewModel PopulateSAPDBTableRequired(string Schema, string Module, SetupCreateViewModel.SAPViewModel model);
        MapCreateViewModel PopulateSAPDBTableRowRequired(string Schema, string Module, SetupCreateViewModel.SAPViewModel model);
        bool Update_EmailSetup(EmailSetup addon, int id);

        bool Update_APISetup(APISetup addon, int id);
        bool Update_Query(QueryManager addon, int id);

        bool Update_QueryMap(QueryManagerMap map, int id, List<string[]> StoreHeaderVal);
        int SaveChanges();

        SetupCreateViewModel GetAPIMethod(string code);
        MapCreateViewModel GetQueries();
        List<SetupCreateViewModel.SAPViewModel> GetSAPSetups();
    }
}
