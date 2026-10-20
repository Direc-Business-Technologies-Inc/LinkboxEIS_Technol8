//using SAPbobsCOM;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace DataAccessLayer.Class
{
    public class SboCred
    {
        //public static Company oCompany;
        public static string password = "B1Admin";


        public static long lRetCode { get; set; }

        //public static void ConnectViaDI()
        //{
        //    credential - papalitan
        //     1.Connect to SAP using DI API Liberary
        //   lRetCode = ConnectViaDI("123.123.123.1:30000",
        //                "123.123.123.2",
        //                "sa",
        //                "B1Admin",
        //                BoDataServerTypes.dst_MSSQL2014,
        //                "RICHFIELD",
        //                "manager",
        //                "B1Admin");
        //    2.Get the return value after attempt login
        //    DIErrorHandler("Connection via DI.");
        //}

        /// <summary>
        /// For DI API Login
        /// </summary>
        /// <param name="oLicenseServer"></param>
        /// <param name="oSAPServer"></param>
        /// <param name="oDBUserName"></param>
        /// <param name="oDBPassword"></param>
        /// <param name="oServerType"></param>
        /// <param name="oCompanyDB"></param>
        /// <param name="oUserName"></param>
        /// <param name="oPassword"></param>
        /// <returns>return long value</returns>
        //public void ConnectViaDI(string oLicenseServer,
        //                                string oSAPServer,
        //                                string oDBUserName,
        //                                string oDBPassword,
        //                                BoDataServerTypes oServerType,
        //                                string oCompanyDB,
        //                                string oUserName,
        //                                string oPassword)
        //{
        //        oCompany = new Company()
        //        {
        //            LicenseServer = oLicenseServer,
        //            Server = oSAPServer,
        //            language = BoSuppLangs.ln_English,
        //            UseTrusted = false,
        //            DbUserName = oDBUserName,
        //            DbPassword = oDBPassword,
        //            DbServerType = oServerType,
        //            CompanyDB = oCompanyDB,
        //            UserName = oUserName,
        //            Password = oPassword
        //        };
        //    oCompany.Connect();
        //}
        //public static void DIErrorHandler(string operation)
        //{
        //    string msg = $"{operation} Success.";
        //    if (lRetCode != 0)
        //    {
        //        int errcode;
        //        string errmsg = "";
        //        oCompany.GetLastError(out errcode, out errmsg);
        //        msg = $"{operation} operation failed. ErrCode: {errcode}. ErrMsg: {errmsg}.";
        //    }
        //    //MessageBox.Show(msg);
        //}
        public class SldCred
        {
            public static string SAPDatabase { get; set; }
            public static string SAPUserID { get; set; }
            public static string SAPPassword { get; set; }
            public static string strCurrentServiceURL { get; set; }
            public static string SessionId { get; set; }
            public static string HttpsUrl { get; set; }
            public static string SAPServer { get; set; }
            public static string SAPDBUserId { get; set; }
            public static string SAPDBPassword { get; set; }
            public static string SAPPort { get; set; }
            public static string SAPSldServer { get; set; }
            public static string SAPHanaTag { get; set; }
            public static JObject JsonData { get; set; }
        }
    }
}