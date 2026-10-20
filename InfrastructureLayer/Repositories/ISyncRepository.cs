using DomainLayer;
using DomainLayer.ViewModels;
using System.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DomainLayer.Models;

namespace InfrastructureLayer.Repositories
{
    public interface ISyncRepository
    {
        SyncViewModel View_Sync();

        int SaveChanges();
        bool Create_SyncSetup(Sync sync, string check, int id);

        SyncViewModel Find_SyncSteup(int id);
        bool Update_SyncSetup(Sync sync, string check, int id);
        bool Create_Query(Query query, int id);
        SyncViewModel Find_Query(int id);
        bool Update_Query(QueryManager query, int id);
        bool Create_SyncQuery(SyncQuery syncquery,int syncid,int queryid,int id);

        bool Create_CrystalExtract(string crystalCode, string selectQueryCode, int apiid);
        bool Create_ReportExtract(CrystalExtractSetup syncquery,int id);


        SyncViewModel Find_SyncQuery(int id);

        SyncViewModel Find_CrystalExtract(int id);
        SyncViewModel Find_Document(int id);

        SyncViewModel Find_QueryManager(int id);

        bool Update_SyncQuery(SyncQuery syncquery, int id);

        bool Update_CrystalExtract(CrystalExtractSetup syncquery, int id);

        SyncViewModel Validate_Sync(string code);

        SyncViewModel Get_SyncQuery(int id);


        DataTable Fill_DataTable(SyncViewModel syncquery);
        void Export(DataTable dt, SyncViewModel syncquery);
    }
}
