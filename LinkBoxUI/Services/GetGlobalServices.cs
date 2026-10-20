using DataAccessLayer.Class;
using DomainLayer.ViewModels;
using InfrastructureLayer.Repositories;
using LinkBoxUI.Context;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Web;

namespace LinkBoxUI.Services
{
    public class GetGlobalServices : IGetGlobalServices
    {
        private readonly LinkboxDb _context = new LinkboxDb();
        public DataAccess da = new DataAccess();
        public SAPAccess sapAces = new SAPAccess();

        public DataTable GetData(string apiRoute)
        {
            DataTable dt = new DataTable();

            var model = new SetupCreateViewModel.APIQuerySetup();
            model = _context.QueryManager.Join(_context.APIManager,
                a => a.QueryCode, b => b.QueryCode, (a, b) => new { a, b })
                .Join(_context.SAPSetup, ab => ab.a.ConnectionString, s => s.SAPId.ToString(), (ab, s) => new { ab, s })
                .Where(x => x.ab.b.APICode == _context.APISetups.Where(z=>z.APIURL.ToLower().Contains(apiRoute.ToLower()))
                .Select(a=>a.APICode).FirstOrDefault())
                .Select(x => new SetupCreateViewModel.APIQuerySetup
                {
                    QueryCode = x.ab.a.QueryCode,
                    QueryString = x.ab.a.QueryString,
                    SAPDBVersion = x.s.SAPDBVersion,
                    SAPLicensePort = x.s.SAPLicensePort,
                    SAPServerName = x.s.SAPServerName,
                    SAPIPAddress = x.s.SAPIPAddress,
                    SAPDBName = x.s.SAPDBName,
                    SAPDBPort = x.s.SAPDBPort,
                    SAPDBuser = x.s.SAPDBuser,
                    SAPDBPassword = x.s.SAPDBPassword,
                    SAPUser = x.s.SAPUser,
                    SAPPassword = x.s.SAPPassword
                }).FirstOrDefault();

            if (model != null)
            {
                dt = model.SAPDBVersion.ToLower().Contains("hana") ? DataAccess.SelectHana(QueryAccess.HANA_conString(model.SAPIPAddress,
                    model.SAPDBuser, model.SAPDBPassword, model.SAPDBName), model.QueryString)
                :
                DataAccess.Select(QueryAccess.MSSQL_conString(model.SAPIPAddress,
                    model.SAPDBuser, model.SAPDBPassword, model.SAPDBName), model.QueryString);
            }

            return dt;
        }

    }
}