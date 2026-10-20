using MSXML2;
using Newtonsoft.Json.Linq;
using DomainLayer.ViewModels;
//using SAPbobsCOM;
using System;
using System.Data;
using System.Text;
using Microsoft.VisualBasic.CompilerServices;
using Microsoft.VisualBasic;
using System.Net;
using System.IO;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace DataAccessLayer.Class
{
    public class SAPAccess
    {
        #region SLD_Variable
        ////Initialization of ServiceLayer variable
        private static ServerXMLHTTP60 ServiceLayer { get; set; }
        //private static XMLHTTP60 ServiceLayer { get; set; }
        public static string sRetMsg { get; set; }
        #endregion

        #region DI_Variable
        ////Initialization of DI API variable
        //public static Company oCompany { get; set; }
        //static Documents oDoc { get; set; }
        //public static long lRetCode { get; set; }
        //public static BoDataServerTypes oSapServerType;
        //public static BoObjectTypes oSapObjectType;
        #endregion

        #region DI_API
        ////POSTING THRU SAPBobSCOM

        //public static void ConnectViaDI(string oLicenseServer,
        //                               string oSAPServer,
        //                               string oDBUserName,
        //                               string oDBPassword,
        //                               BoDataServerTypes oServerType,
        //                               string oCompanyDB,
        //                               string oUserName,
        //                               string oPassword)
        //{
        //    oCompany = new Company()
        //    {
        //        LicenseServer = oLicenseServer,
        //        Server = oSAPServer,
        //        language = BoSuppLangs.ln_English,
        //        UseTrusted = false,
        //        DbUserName = oDBUserName,
        //        DbPassword = oDBPassword,
        //        DbServerType = oServerType,
        //        CompanyDB = oCompanyDB,
        //        UserName = oUserName,
        //        Password = oPassword
        //    };
        //    lRetCode = oCompany.Connect();
        //    DIErrorHandler("connect", lRetCode);
        //}

        //public static void DIErrorHandler(string operation, long r)
        //{
        //    string msg = $"{operation} Success.";
        //    if (r != 0)
        //    {
        //        int errcode;
        //        string errmsg = "";
        //        oCompany.GetLastError(out errcode, out errmsg);
        //        msg = $"{operation} operation failed. ErrCode: {errcode}. ErrMsg: {errmsg}.";
        //        // add return error to addon db
        //    }
        //}

        //public static void CreateDocument(Tuple<DataTable, DataTable> Data, BoObjectTypes document)
        //{
        //    foreach (DataRow head in Data.Item1.Rows)
        //    {
        //        oDoc = oCompany.GetBusinessObject(document);
        //        oDoc.CardCode = head[0].ToString();
        //        oDoc.DocDate = Convert.ToDateTime(head[2].ToString());

        //        //oDoc.DocDueDate = DateTime.Now.AddDays(7);
        //        //oDoc.RequriedDate = DateTime.Now.AddDays(7);
        //        oDoc.UserFields.Fields.Item("U_Company").Value = "Shawarma Shack Fastfood Corporation";
        //        oDoc.UserFields.Fields.Item("U_OrderClass").Value = "2";
        //        foreach (DataRow row in Data.Item2.Rows)
        //        {
        //            var oLines = oDoc.Lines;
        //            if (head[0].ToString() == row[0].ToString() && head[3].ToString() == row[3].ToString() && row[7].ToString() == "Yes")
        //            {
        //                oLines.ItemCode = row[9].ToString();
        //                oLines.Quantity = Convert.ToDouble(row[11].ToString());
        //                oLines.UnitPrice = Convert.ToDouble(row[13].ToString());
        //                oLines.PriceAfterVAT = Convert.ToDouble(row[14].ToString());
        //                oLines.LineTotal = Convert.ToDouble(row[16].ToString());
        //                //oLines.GrossTotal = Convert.ToDouble(row[19].ToString());
        //                // freight
        //                //oLines.UoMEntry = Convert.ToInt32(row[12].ToString());
        //                oLines.Add();
        //            }

        //        }
        //        long iret = oDoc.Add();
        //        DIErrorHandler("Create Document.", iret);
        //        oCompany.StartTransaction();
        //        oCompany.GetNewObjectCode(out string sDocEntry);
        //        CopyDocument(int.Parse(sDocEntry), document, BoObjectTypes.oInvoices);
        //        if (lRetCode == 0)
        //        { oCompany.EndTransaction(BoWfTransOpt.wf_Commit); }
        //        else
        //        { oCompany.EndTransaction(BoWfTransOpt.wf_RollBack); }

        //    }
        //    //long iret = oDoc.Add();
        //    //DIErrorHandler("Create Document.",iret);

        //}


        //public static void CreateGRPO(Tuple<DataTable, DataTable> Data, BoObjectTypes document)
        //{
        //    foreach (DataRow head in Data.Item1.Rows)
        //    {
        //        oDoc = oCompany.GetBusinessObject(BoObjectTypes.oPurchaseDeliveryNotes);
        //        oDoc.CardCode = head[0].ToString();
        //        oDoc.UserFields.Fields.Item("U_DocNo").Value = "";
        //        oDoc.UserFields.Fields.Item("U_PrepBy").Value = "";
        //        oDoc.UserFields.Fields.Item("U_InTransNum").Value = "";
        //        oDoc.DocDueDate = DateTime.Now.AddDays(7);
        //        oDoc.DocDate = DateTime.Now.AddDays(7);
        //        oDoc.TaxDate = DateTime.Now.AddDays(7);
        //        foreach (DataRow row in Data.Item2.Rows)
        //        {
        //            var oLines = oDoc.Lines;
        //            if (row[7].ToString() == "D")
        //            {

        //                oLines.AccountCode = row[9].ToString();
        //                oLines.LineTotal = Convert.ToDouble(row[13].ToString());
        //                oLines.UserFields.Fields.Item("U_API_Adress").Value = "";
        //                oLines.UserFields.Fields.Item("U_API_Vendor").Value = "";
        //                oLines.UserFields.Fields.Item("U_API_TIN").Value = "";
        //                oLines.Add();
        //            }

        //        }
        //        long iret1 = oDoc.Add();
        //        DIErrorHandler(oCompany.GetLastErrorDescription(), iret1);


        //        //CopyDocument(int.Parse(sDocEntry), document, BoObjectTypes.oInvoices);
        //        //if (lRetCode == 0)
        //        //{ oCompany.EndTransaction(BoWfTransOpt.wf_Commit); }
        //        //else
        //        //{ oCompany.EndTransaction(BoWfTransOpt.wf_RollBack); }

        //    }
        //    long iret = oDoc.Add();
        //    DIErrorHandler("Create Document.", iret);

        //}

        //static void CopyDocument(int oBaseEntry,
        //                 BoObjectTypes baseType,
        //                 BoObjectTypes targetType)
        //{
        //    Documents baseDoc = oCompany.GetBusinessObject(baseType);

        //    if (baseDoc.GetByKey(oBaseEntry))
        //    {
        //        Documents targetDoc = oCompany.GetBusinessObject(targetType);
        //        targetDoc.CardCode = baseDoc.CardCode;
        //        targetDoc.DocDueDate = baseDoc.DocDueDate;
        //        var baseLines = baseDoc.Lines;
        //        var targetLines = targetDoc.Lines;

        //        for (int i = 0; i < baseLines.Count; i++)
        //        {
        //            targetLines.BaseType = SAPObjectTypes(baseType.ToString());
        //            targetLines.BaseEntry = oBaseEntry;
        //            targetLines.BaseLine = i;
        //            targetLines.Add();
        //        }

        //        lRetCode = targetDoc.Add();
        //        DIErrorHandler("Copy Document.", lRetCode);
        //    }
        //    else
        //    {
        //        DIErrorHandler("Error", oBaseEntry);
        //    }
        //}

        //public static BoDataServerTypes SAPServerType(string ServerType)
        //{
        //    int i = -1;
        //    Array value = Enum.GetValues(typeof(BoDataServerTypes));
        //    foreach (var item in Enum.GetValues(typeof(BoDataServerTypes)))
        //    {
        //        i++;
        //        if (ServerType == item.ToString())
        //        {
        //            oSapServerType = (BoDataServerTypes)value.GetValue(i);
        //            break;
        //        }
        //    }
        //    return oSapServerType;
        //}

        //public static int SAPObjectTypes(string ObjectType)
        //{
        //    int i = -1;
        //    int objectid = 0;
        //    Array value = Enum.GetValues(typeof(BoObjectTypes));
        //    foreach (var item in Enum.GetValues(typeof(BoObjectTypes)))
        //    {
        //        i++;
        //        if (ObjectType == item.ToString())
        //        {

        //            objectid = (int)(BoDataServerTypes)value.GetValue(i);
        //            //objectid = i;
        //            break;
        //        }
        //    }
        //    return objectid;
        //}

        #endregion

        #region SLD_API
        ////POSTING THRU SERVICE LAYER DATA       
        public void SaveCredentials(AuthenticationCredViewModel creds)
        {
            SboCred.SldCred.HttpsUrl = string.IsNullOrEmpty(creds.URL) ? "http://" : creds.URL + "://";
            SboCred.SldCred.SAPSldServer = creds.SAPSldServer;
            SboCred.SldCred.SAPServer = creds.SAPServer;
            SboCred.SldCred.SAPPort = creds.Port;
            SboCred.SldCred.SAPHanaTag = SboCred.SldCred.SAPSldServer + ":" + SboCred.SldCred.SAPPort + "/b1s/v1/";
            SboCred.SldCred.SAPDatabase = creds.SAPDatabase;
            SboCred.SldCred.SAPUserID = creds.SAPUserID;
            SboCred.SldCred.SAPPassword = creds.SAPPassword;
            SboCred.SldCred.SAPDBUserId = creds.SAPDBUserId;
            SboCred.SldCred.SAPDBPassword = creds.SAPDBPassword;
            SboCred.SldCred.strCurrentServiceURL = SboCred.SldCred.HttpsUrl + SboCred.SldCred.SAPHanaTag;
            //// UNUSED VARIABLE
            //SboCred.SldCred.JsonData = creds.JsonData != null ? creds.JsonData : creds.JsonString != null ? JObject.Parse(creds.JsonString) : null;
        }
        private static void ServiceURL()
        {
            //try
            //{
                
                SboCred.SldCred.strCurrentServiceURL = $"{SboCred.SldCred.HttpsUrl}{SboCred.SldCred.SAPHanaTag}";
                const string httpStr = "http://";
                const string httpsStr = "https://";
                if (!SboCred.SldCred.strCurrentServiceURL.StartsWith(httpStr, true, null) &&
                    !SboCred.SldCred.strCurrentServiceURL.StartsWith(httpsStr, true, null))
                {
                    SboCred.SldCred.strCurrentServiceURL = httpStr + SboCred.SldCred.strCurrentServiceURL;
                }

                if (ServiceLayer == null)
                {
                    ServiceLayer = new ServerXMLHTTP60();
                }

               // lock () { }
                ServiceLayer.setOption(SERVERXMLHTTP_OPTION.SXH_OPTION_IGNORE_SERVER_SSL_CERT_ERROR_FLAGS, 13056);

            ///}
            //catch (Exception e) { 

            // }   
                
        }


        public static bool LoginAction()
        {
            string err;
            bool result = true;
            var json = new StringBuilder();
            json.AppendLine("{");
            json.AppendLine($@" ""CompanyDB"" : ""{SboCred.SldCred.SAPDatabase}"",");
            json.AppendLine($@" ""UserName"" : ""{SboCred.SldCred.SAPUserID}"",");
            json.AppendLine($@" ""Password"" : ""{SboCred.SldCred.SAPPassword}""");
            json.AppendLine("}");

            try
            {
                ServiceURL();
                lock (ServiceLayer)
                {
                    ServiceLayer.open("POST", $@"{SboCred.SldCred.strCurrentServiceURL}Login");
                    ServiceLayer.send(json.ToString());

                    if (ServiceLayer.responseText.ToLower().Contains("400 bad request"))
                    {
                        ServiceURL();
                        SboCred.SldCred.strCurrentServiceURL = SboCred.SldCred.strCurrentServiceURL.Replace("http://", "https://");
                        ServiceLayer.open("POST", $@"{SboCred.SldCred.strCurrentServiceURL}Login");
                        ServiceLayer.send(json.ToString());
                    }

               }
            }
            catch (Exception ex)
            {
                if (ex.Message.ToLower().Contains("terminated abnormally") || ex.Message.ToLower().Contains("could not be established\r\n") || ex.Message.ToLower().Contains("this method cannot be called after the send method has been called"))
                {
                    ServiceURL();
                    SboCred.SldCred.strCurrentServiceURL = SboCred.SldCred.strCurrentServiceURL.Replace("http://", "https://");
                    ServiceLayer.open("POST", $@"{SboCred.SldCred.strCurrentServiceURL}Login");
                    ServiceLayer.send(json.ToString());
                }
                else
                {
                    err = ex.Message;
                    result = false;
                    return result;
                }
            }

            string ret = GetJsonValue(ServiceLayer.responseText, "SessionId");

            if (string.IsNullOrEmpty(ret) || ret.ToLower().Contains("error"))
            {
                err = GetJsonError(ServiceLayer.responseText);
                result = false;
            }
            else
            {
                result = ret.Contains("-");
                err = ret;
                SboCred.SldCred.SessionId = ret;
            }

            return result;
        }

        //public static bool egen_LoginAction()
        //{
        //    ServiceURL();
        //    string err;
        //    bool result = true;
        //    var json = new StringBuilder();
        //    json.AppendLine("{");
        //    json.AppendLine($@" ""CompanyDB"" : ""{SboCred.SldCred.SAPDatabase}"",");
        //    json.AppendLine($@" ""UserName"" : ""{SboCred.SldCred.SAPUserID}"",");
        //    json.AppendLine($@" ""Password"" : ""{SboCred.SldCred.SAPPassword}""");
        //    json.AppendLine("}");

        //    try
        //    {

        //        ServiceLayer.open("POST", $@"{SboCred.SldCred.strCurrentServiceURL}Login");
        //        ServiceLayer.send(json.ToString());

        //        if (ServiceLayer.responseText.ToLower().Contains("400 bad request"))
        //        {
        //            ServiceURL();
        //            SboCred.SldCred.strCurrentServiceURL = SboCred.SldCred.strCurrentServiceURL.Replace("http://", "https://");
        //            ServiceLayer.open("POST", $@"{SboCred.SldCred.strCurrentServiceURL}Login");
        //            ServiceLayer.send(json.ToString());
        //        }

        //    }
        //    catch (Exception ex)
        //    {
        //        if (ex.Message.ToLower().Contains("terminated abnormally") || ex.Message.ToLower().Contains("could not be established\r\n") || ex.Message.ToLower().Contains("this method cannot be called after the send method has been called"))
        //        {
        //            ServiceURL();
        //            SboCred.SldCred.strCurrentServiceURL = SboCred.SldCred.strCurrentServiceURL.Replace("http://", "https://");
        //            ServiceLayer.open("POST", $@"{SboCred.SldCred.strCurrentServiceURL}Login");
        //            ServiceLayer.send(json.ToString());
        //        }
        //        else
        //        {
        //            err = ex.Message;
        //            result = false;
        //            return result;
        //        }
        //    }

        //    string ret = GetJsonValue(ServiceLayer.responseText, "SessionId");

        //    if (string.IsNullOrEmpty(ret) || ret.ToLower().Contains("error"))
        //    {
        //        err = GetJsonError(ServiceLayer.responseText);
        //        result = false;
        //    }
        //    else
        //    {
        //        result = ret.Contains("-");
        //        err = ret;
        //        SboCred.SldCred.SessionId = ret;
        //    }

        //    return result;
        //}
        private static string GetJsonString(string ret, string tag)
        {
            var startTag = "{";
            int startIndex = ret.IndexOf(startTag) + startTag.Length;
            int endIndex = ret.IndexOf("}", startIndex);
            return ret.Substring(startIndex, endIndex - startIndex);
        }
        private static string GetJsonValue(string json, string value)
        {
            try
            {
                if (json != null)
                {
                    JObject err = JObject.Parse(json);
                    if (err.ToString().ToLower().Contains($@"""error"":"))
                    {
                        return $"error : {GetJsonError(err.ToString())}";
                    }
                    else
                    {
                        return (string)err[value];
                    }
                }
                else
                {
                    return "";
                }
            }
            catch
            {
                if (json.ToLower().Contains("error"))
                {
                    string retJson = GetJsonString(json, "");
                    var sbJson = new StringBuilder();
                    sbJson.Append("{" + retJson + "}}}");
                    return GetJsonError(sbJson.ToString());
                }
                else { return "Operation completed successfully"; }
            }
        }
        private static string GetJsonError(string json)
        {
            JObject err = JObject.Parse(json);
            return (string)err["error"]["message"]["value"];
        }

        public string SendSLData(AuthenticationCredViewModel auth)
        {
            string ret = "";
            try
            {
                if (auth.JsonData != null)
                {
                    ret = SBOResponse(auth.Method, auth.Action, auth.JsonData.ToString(), "DocEntry");
                }
                else
                {

                    ret = SBOResponse(auth.Method, auth.Action, auth.JsonString.ToString(), "DocEntry");
                }
            }
            catch (Exception ex)
            {
                ret = ex.Message;
            }

            return ret;
        }

        //public string egen_SendSLData(AuthenticationCredViewModel auth)
        //{
        //    string ret = "";
        //    try
        //    {
        //        ret = SBOResponse(auth.Method, auth.Action, auth.JsonString.ToString(), "DocEntry");
        //    }
        //    catch (Exception ex)
        //    {
        //        ret = ex.Message;
        //    }

        //    return ret;
        //}

        //public string ReportSendSLData(AuthenticationCredViewModel auth)
        //{
        //    string ret = "";
        //    try
        //    {
        //        if (auth.JsonData != null)
        //        {
        //            ret = SBOResponse(auth.Method, auth.Action, auth.JsonData.ToString(), "DocEntry");
        //        }
        //        else
        //        {
        //            ret = SBOResponse(auth.Method, auth.Action, auth.JsonString.ToString(), "DocEntry");
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        ret = ex.Message;
        //    }

        //    return ret;
        //}

        public static string SBOResponse(string sMethod, string sModule, string sJson, string sRetValue)
        {
            //var output = true;
            string output = "";
            string url = SboCred.SldCred.strCurrentServiceURL;
            try
            {
                ServiceURL();
                lock (ServiceLayer)
                {

                    ServiceLayer.open(sMethod, $"{url}{sModule}");

                    if (sModule.Contains("Attachments2"))
                    {
                        ServiceLayer.setRequestHeader("Content-Type", "multipart/form-data");
                    }

                    ServiceLayer.send(sJson);
                    if (ServiceLayer.responseText.ToLower().Contains("400 bad request"))
                    {
                        ServiceURL();
                        SboCred.SldCred.strCurrentServiceURL = SboCred.SldCred.strCurrentServiceURL.Replace("http://", "https://");
                        ServiceLayer.open(sMethod, $@"{url}");
                        ServiceLayer.send(sJson.ToString());
                    }

                    var response = ServiceLayer.responseText;

                    if (sMethod == "GET" && sJson == "{}")
                    {
                        output = response.ToString();
                    }
                    else
                    {
                        if (sModule.Equals("SBOBobService_GetCurrencyRate"))
                        {
                            return response;
                        }
                        else
                        {
                            output = GetJsonValue(response, sRetValue);

                        }
                    }
                }

            }
            catch (Exception ex)
            {
                if (ex.Message.ToLower().Contains("terminated abnormally"))
                {
                    ServiceURL();
                    SboCred.SldCred.strCurrentServiceURL = SboCred.SldCred.strCurrentServiceURL.Replace("http://", "https://");
                    ServiceLayer.open("POST", $@"{SboCred.SldCred.strCurrentServiceURL}Login");
                    ServiceLayer.send(sJson.ToString());
                }
                else
                {
                    output = "Error";
                    sRetMsg = ex.Message;
                }
            }


            return output;

        }


        public static string egen_SBOResponse(string sMethod, string sModule, string sJson, string sRetValue)
        {
            //var output = true;
            string output = "";
            string url = SboCred.SldCred.strCurrentServiceURL;

            ServiceURL();
            ServiceLayer.open(sMethod, $"{url}{sModule}");

            try
            {
                ServiceLayer.send(sJson);
                if (ServiceLayer.responseText.ToLower().Contains("400 bad request"))
                {
                    ServiceURL();
                    SboCred.SldCred.strCurrentServiceURL = SboCred.SldCred.strCurrentServiceURL.Replace("http://", "https://");
                    ServiceLayer.open(sMethod, $@"{url}");
                    ServiceLayer.send(sJson.ToString());
                }

                var response = ServiceLayer.responseText;

                output = GetJsonValue(response, sRetValue);


            }
            catch (Exception ex)
            {
                if (ex.Message.ToLower().Contains("terminated abnormally"))
                {
                    ServiceURL();
                    SboCred.SldCred.strCurrentServiceURL = SboCred.SldCred.strCurrentServiceURL.Replace("http://", "https://");
                    ServiceLayer.open("POST", $@"{SboCred.SldCred.strCurrentServiceURL}Login");
                    ServiceLayer.send(sJson.ToString());
                }
                else
                {
                    output = "Error";
                    sRetMsg = ex.Message;
                }
            }


            return output;

        }

        #endregion

        #region API_Response
        public string APIResponse(string sMethod, string sURL, string sModule, string sJson, string sRetValue, string user, string pwd, int limit, List<ApiParameterViewModel.ApiParameter> parameters)
        {
            string output = "";
            var response = SendJson(sMethod, sURL, sJson, user, pwd, limit, parameters);
            try
            {
                if (string.IsNullOrEmpty(sRetValue))
                {
                    output = response.ToString();
                }
                else
                {
                    output = GetJsonValue(response, sRetValue);
                }

            }
            catch (Exception ex)
            {
                output = "Error";
                sRetMsg = ex.Message;
            }

            return output;
        }

        public static string SendJson(string method, string url, string Json, string user, string pwd, int limit, List<ApiParameterViewModel.ApiParameter> parameters)
        {
            var result = "";
            if (parameters == null) { parameters = new List<ApiParameterViewModel.ApiParameter>(); }
            foreach (var param in parameters)
            {
                url = (url.Contains("?") ? url + $@"&{param.APIParameter}={param.APIParamValue}" : url + $@"?{param.APIParameter}={param.APIParamValue}");
            }

            if (limit > 0)
            { url = url.Contains("?") ? $@"{url}&limit=1" : $@"{url}?limit=1"; }
            var httpWebRequest = (HttpWebRequest)WebRequest.Create(url);
            HttpWebResponse httpResponse;
            try
            {
                httpWebRequest.Method = string.IsNullOrEmpty(method) ? "POST" : method;
                httpWebRequest.Accept = "*/*";
                httpWebRequest.ContentType = "application/json";
                httpWebRequest.Credentials = new NetworkCredential(user, pwd);
                //httpWebRequest.Timeout = 1048576;
                var jsonparse = string.IsNullOrEmpty(Json) ? "" : JObject.Parse(Json).ToString();
                if (method.ToLower().Contains("post") || method.ToLower().Contains("put"))
                {
                    var data = Encoding.ASCII.GetBytes(jsonparse.ToString());
                    httpWebRequest.ContentLength = data.Length;
                    using (var streamwriter = httpWebRequest.GetRequestStream())
                    {
                        streamwriter.Write(data, 0, data.Length);
                        streamwriter.Flush();
                    }
                }

                httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
                using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
                {
                    result = streamReader.ReadToEnd();
                }
                return result;
            }
            catch (WebException webex)
            {
                string exresult = "";
                using (var stream = webex.Response.GetResponseStream())
                using (var reader = new StreamReader(stream))
                {
                    exresult = reader.ReadToEnd();
                }
                return $@"error: {exresult}";
            }
        }
        public string XmlPostJson(string method, string url, string Json)
        {
            ServiceURL();
            string result;
            try
            {
                ServiceLayer.open(method, $@"{url}");
                ServiceLayer.send(Json.ToString());
                if (ServiceLayer.responseText.ToLower().Contains("400 bad request"))
                {
                    ServiceURL();
                    SboCred.SldCred.strCurrentServiceURL = SboCred.SldCred.strCurrentServiceURL.Replace("http://", "https://");
                    ServiceLayer.open(method, $@"{url}");
                    ServiceLayer.send(Json.ToString());
                }
            }
            catch (Exception ex)
            {
                if (ex.Message.ToLower().Contains("terminated abnormally"))
                {
                    ServiceURL();
                    SboCred.SldCred.strCurrentServiceURL = SboCred.SldCred.strCurrentServiceURL.Replace("http://", "https://");
                    ServiceLayer.open("POST", $@"{SboCred.SldCred.strCurrentServiceURL}Login");
                    ServiceLayer.send(Json.ToString());
                }
                else
                {
                    return ex.Message;
                }
            }
            result = ServiceLayer.responseText;

            return result;
        }
        #endregion


        public static string SanitizeFilename(string filename)
        {
            char[] invalidChars = Path.GetInvalidFileNameChars();
            foreach (char c in invalidChars)
            {
                filename = filename.Replace(c, '_'); // Replace invalid characters with an underscore or another valid character
            }
            return filename;
        }

    }
}
