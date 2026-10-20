using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics.PerformanceData;
using System.Linq;
using System.Text;
using System.Web;
using DocumentFormat.OpenXml.Spreadsheet;
using DomainLayer.ViewModels;
using LinkBoxUI.Helpers;
using MSXML2;
using Microsoft.Identity.Client;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net.Http.Headers;
using DomainLayer;

namespace LinkBoxUI.Helpers
{
    public static class PostingHelpers
    {
        //public static XMLHTTP60 ServiceLayer = new XMLHTTP60();
        public static ServerXMLHTTP60 ServiceLayer = new ServerXMLHTTP60();
        public static SqlHelpers sql = new SqlHelpers();
        public static bool LoginAction(PostingViewModel Creds)
        {
            try
            {

                string err;
                bool result = true;
                var json = new StringBuilder();
                json.AppendLine("{");
                json.AppendLine($@" ""CompanyDB"" : ""{Creds.CredentialDetails.SAPDBName}"",");
                json.AppendLine($@" ""UserName"" : ""{Creds.CredentialDetails.SAPUser}"",");
                json.AppendLine($@" ""Password"" : ""{Creds.CredentialDetails.SAPPassword}""");
                json.AppendLine("}");

                ServiceLayer.open("POST", $@"{ServiceURL(Creds.CredentialDetails.SAPSldServer, Creds.CredentialDetails.SAPLicensePort.ToString())}Login");
                //ServiceLayer.setOption(SERVERXMLHTTP_OPTION.SXH_OPTION_IGNORE_SERVER_SSL_CERT_ERROR_FLAGS, 13056);
                try
                {
                    ServiceLayer.send(json.ToString());
                    var response = ServiceLayer.responseText;
                    if (response.ToLower().Contains("bad request"))
                    {
                        string url = ServiceURL(Creds.CredentialDetails.SAPSldServer, Creds.CredentialDetails.SAPLicensePort.ToString());
                        url = url.Replace("http", "https");
                        ServiceLayer.open("POST", $@"{url}Login");
                        ServiceLayer.send(json.ToString());
                    }
                }
                catch (Exception ex)
                {
                    if (ex.Message.ToLower().Contains("terminated abnormally"))
                    {
                        string url = ServiceURL(Creds.CredentialDetails.SAPSldServer, Creds.CredentialDetails.SAPLicensePort.ToString());
                        url = url.Replace("http", "https");
                        ServiceLayer.open("POST", $@"{url}Login");
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

                if (string.IsNullOrEmpty(ret))
                {
                    err = GetJsonError(ServiceLayer.responseText);
                    result = false;
                }
                else
                {
                    result = ret.Contains("-");
                    err = ret;
                }

                return result;
            }
            catch (Exception)
            {
                return false;
            }

        }

        public static string ServiceURL(string Server, string Port)
        {
            var url = $"http://{Server}:{Port}/b1s/v1/";
            const string httpStr = "http://";
            const string httpsStr = "https://";
            if (!url.StartsWith(httpStr, true, null) &&
                !url.StartsWith(httpsStr, true, null))
            {
                url = httpStr + url;
            }

            if (ServiceLayer == null)
            { ServiceLayer = new ServerXMLHTTP60(); }
            ServiceLayer.setOption(SERVERXMLHTTP_OPTION.SXH_OPTION_IGNORE_SERVER_SSL_CERT_ERROR_FLAGS, 13056);
            return url;
        }

        public static string GetJsonValue(string json, string value)
        {
            try
            {
                if (json != null)
                {
                    JObject err = JObject.Parse(json);
                    if (err.ToString().Contains("error"))
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
                if (json.Contains("error"))
                {
                    string retJson = GetJsonString(json, "");
                    var sbJson = new StringBuilder();
                    sbJson.Append("{" + retJson + "}}}");
                    return GetJsonError(sbJson.ToString());
                }
                else { return "Operation completed successfully"; }
            }
        }

        public static string GetJsonError(string json)
        {
            JObject err = JObject.Parse(json);
            return (string)err["error"]["message"]["value"];
        }

        public static string GetJsonString(string ret, string tag)
        {
            var startTag = "{";
            int startIndex = ret.IndexOf(startTag) + startTag.Length;
            int endIndex = ret.IndexOf("}", startIndex);
            return ret.Substring(startIndex, endIndex - startIndex);
        }

        public static string SBOResponse(string sMethod, string sModule, string sJson, string sRetValue, PostingViewModel Creds)
        {
            //var output = true;
            string output = "";
            string url = ServiceURL(Creds.CredentialDetails.SAPSldServer, Creds.CredentialDetails.SAPLicensePort.ToString());
            ServiceLayer.open(sMethod, $"{url}{sModule}");
            //ServiceLayer.setOption(SERVERXMLHTTP_OPTION.SXH_OPTION_IGNORE_SERVER_SSL_CERT_ERROR_FLAGS, 13056);
            //ServiceLayer.setTimeouts(1000000, 1000000, 1000000, 1000000);
            if (sModule.Contains("Attachments2"))
            {
                ServiceLayer.setRequestHeader("Content-Type", "multipart/form-data");
            }
            var err = "";
            try
            {
                ServiceLayer.send(sJson);
                var response = ServiceLayer.responseText;
                if (response.ToLower().Contains("bad request"))
                {
                    url = ServiceURL(Creds.CredentialDetails.SAPSldServer, Creds.CredentialDetails.SAPLicensePort.ToString());
                    url = url.Replace("http", "https");
                    ServiceLayer.open(sMethod, $@"{url}{sModule}");
                    ServiceLayer.send(sJson.ToString());
                    response = ServiceLayer.responseText;
                }
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
                err = ex.Message;
            }
            finally
            {
                if (err.ToLower().Contains("terminated abnormally") || err.ToLower().Contains("invalid or unrecognized response"))
                {
                    ServiceLayer = new ServerXMLHTTP60();
                    url = ServiceURL(Creds.CredentialDetails.SAPSldServer, Creds.CredentialDetails.SAPLicensePort.ToString());
                    LoginAction(Creds);
                    url = url.Replace("http", "https");
                    ServiceLayer.open(sMethod, $@"{url}{sModule}");
                    ServiceLayer.send(sJson.ToString());
                    var response = ServiceLayer.responseText;
                    if (string.IsNullOrEmpty(sRetValue))
                    {
                        output = response.ToString();
                    }
                    else
                    {
                        output = GetJsonValue(response, sRetValue);
                    }
                }
                else
                {
                    if (!err.Equals(""))
                    {
                        output = $"error : {err}";
                    }
                }
            }
            return output;
        }

        public static bool IsValidJson(string strInput)
        {
            if (string.IsNullOrWhiteSpace(strInput)) { return false; }
            strInput = strInput.Trim();
            if ((strInput.StartsWith("{") && strInput.EndsWith("}")) || //For object
                (strInput.StartsWith("[") && strInput.EndsWith("]"))) //For array
            {
                try
                {
                    var obj = JToken.Parse(strInput);
                    return true;
                }
                catch (JsonReaderException)
                {
                    //Exception in parsing json
                    return false;
                }
                catch (Exception) //some other exception
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

    }

    public class PostHelper
    {
        public ServerXMLHTTP60 ServiceLayer2Auto = new ServerXMLHTTP60();

        public string Posting4Automation(string sMethod, string sModule, string sJson, string sRetValue, PostingViewModel Creds)
        {
            try
            {
                //PostHelper ph = new PostHelper();
                ServiceLayer2Auto = new ServerXMLHTTP60();
                ServiceLayer2Auto.setOption(SERVERXMLHTTP_OPTION.SXH_OPTION_IGNORE_SERVER_SSL_CERT_ERROR_FLAGS, 13056);

                var url = $"http://{Creds.CredentialDetails.SAPSldServer}:{Creds.CredentialDetails.SAPLicensePort}/b1s/v1/";
                const string httpStr = "http://";
                const string httpsStr = "https://";
                if (!url.StartsWith(httpStr, true, null) &&
                    !url.StartsWith(httpsStr, true, null))
                {
                    url = httpStr + url;
                }

                string output;
                var json = new StringBuilder();
                json.AppendLine("{");
                json.AppendLine($@" ""CompanyDB"" : ""{Creds.CredentialDetails.SAPDBName}"",");
                json.AppendLine($@" ""UserName"" : ""{Creds.CredentialDetails.SAPUser}"",");
                json.AppendLine($@" ""Password"" : ""{Creds.CredentialDetails.SAPPassword}""");
                json.AppendLine("}");

                ServiceLayer2Auto.open("POST", $@"{url}Login", false);
                try
                {
                    ServiceLayer2Auto.send(json.ToString());
                    var response = ServiceLayer2Auto.responseText;
                    if (response.ToLower().Contains("bad request"))
                    {
                        url = url.Replace("http", "https");
                        ServiceLayer2Auto.open("POST", $@"{url}Login", false);
                        ServiceLayer2Auto.send(json.ToString());
                    }
                }
                catch (Exception ex)
                {
                    if (ex.Message.ToLower().Contains("terminated abnormally"))
                    {
                        url = url.Replace("http", "https");
                        ServiceLayer2Auto.open("POST", $@"{url}Login", false);
                        ServiceLayer2Auto.send(json.ToString());
                    }
                    else
                    {
                        return ex.Message;
                    }
                }

                string ret = GetJsonValue(ServiceLayer2Auto.responseText, "SessionId");

                if (string.IsNullOrEmpty(ret))
                {
                    output = GetJsonError(ServiceLayer2Auto.responseText);
                }
                else
                {
                    ServiceLayer2Auto.open(sMethod, $@"{url}{sModule}", false);
                    ServiceLayer2Auto.send("{" + sJson.ToString() + "}");
                    var response = ServiceLayer2Auto.responseText;

                    if (string.IsNullOrEmpty(sRetValue))
                    {
                        output = response.ToString();
                    }
                    else
                    {
                        output = GetJsonValue(response, sRetValue);
                    }
                }

                return output;
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }
        public string Posting5Automation(string sMethod, string sModule, string sJson, string sRetValue, PostingViewModel Creds, string CardCode, string IPDocEntry, bool IP_toBePosted, string OPDocEntry)
        {
            try
            {
                //PostHelper ph = new PostHelper();
                ServiceLayer2Auto = new ServerXMLHTTP60();
                ServiceLayer2Auto.setOption(SERVERXMLHTTP_OPTION.SXH_OPTION_IGNORE_SERVER_SSL_CERT_ERROR_FLAGS, 13056);

                var url = $"http://{Creds.CredentialDetails.SAPSldServer}:{Creds.CredentialDetails.SAPLicensePort}/b1s/v1/";
                const string httpStr = "http://";
                const string httpsStr = "https://";
                if (!url.StartsWith(httpStr, true, null) &&
                    !url.StartsWith(httpsStr, true, null))
                {
                    url = httpStr + url;
                }

                string output;
                var json = new StringBuilder();
                json.AppendLine("{");
                json.AppendLine($@" ""CompanyDB"" : ""{Creds.CredentialDetails.SAPDBName}"",");
                json.AppendLine($@" ""UserName"" : ""{Creds.CredentialDetails.SAPUser}"",");
                json.AppendLine($@" ""Password"" : ""{Creds.CredentialDetails.SAPPassword}""");
                json.AppendLine("}");

                ServiceLayer2Auto.open("POST", $@"{url}Login", false);
                try
                {
                    ServiceLayer2Auto.send(json.ToString());
                    var response = ServiceLayer2Auto.responseText;
                    if (response.ToLower().Contains("bad request"))
                    {
                        url = url.Replace("http", "https");
                        ServiceLayer2Auto.open("POST", $@"{url}Login", false);
                        ServiceLayer2Auto.send(json.ToString());
                    }
                }
                catch (Exception ex)
                {
                    if (ex.Message.ToLower().Contains("terminated abnormally"))
                    {
                        url = url.Replace("http", "https");
                        ServiceLayer2Auto.open("POST", $@"{url}Login", false);
                        ServiceLayer2Auto.send(json.ToString());
                    }
                    else
                    {
                        return ex.Message;
                    }
                }

                string ret = GetJsonValue(ServiceLayer2Auto.responseText, "SessionId");

                if (string.IsNullOrEmpty(ret))
                {
                    output = GetJsonError(ServiceLayer2Auto.responseText);
                }
                else
                {
                    if (CardCode != "")
                    {
                        ServiceLayer2Auto.open(sMethod, $@"{url}{sModule}('{CardCode}')", false);
                        ServiceLayer2Auto.send(sJson.ToString());
                    }
                    else if (IPDocEntry != "" && IP_toBePosted == false)
                    {

                        ServiceLayer2Auto.open(sMethod, $@"{url}{sModule}({IPDocEntry})/Cancel", false);
                        sJson = "";
                        ServiceLayer2Auto.send(sJson.ToString());

                        string postRes = ServiceLayer2Auto.responseText;

                        if (!postRes.Contains("error"))
                        {
                            if (!OPDocEntry.Equals(""))
                            {
                                ServiceLayer2Auto.open(sMethod, $@"{url}{sModule}({OPDocEntry})/Cancel", false);
                                sJson = "";
                                ServiceLayer2Auto.send(sJson.ToString());
                            }

                        }
                        else
                        {
                            return output = postRes.ToString();
                        }


                    }
                    else if (IPDocEntry == "-1" && IP_toBePosted == true)
                    {
                        ServiceLayer2Auto.open(sMethod, $@"{url}{sModule}", false);
                        ServiceLayer2Auto.send(sJson.ToString());

                        string postRes = ServiceLayer2Auto.responseText;

                        if (!postRes.Contains("error"))
                        {
                            JObject jObject = JObject.Parse(postRes);
                            string DocEntry = (string)jObject["DocEntry"];
                            // Access the DocNum property and save it in a variable

                            ServiceLayer2Auto.open(sMethod, $@"{url}{sModule}({DocEntry})/Cancel", false);
                            sJson = "";
                            ServiceLayer2Auto.send(sJson.ToString());

                            string postReturn = ServiceLayer2Auto.responseText;


                            if (!postReturn.Contains("error"))
                            {
                                if (!OPDocEntry.Equals(""))
                                {
                                    ServiceLayer2Auto.open(sMethod, $@"{url}{sModule}({OPDocEntry})/Cancel", false);
                                    sJson = "";
                                    ServiceLayer2Auto.send(sJson.ToString());
                                }

                            }
                            else
                            {
                                return output = postReturn.ToString();
                            }

                        }
                        else
                        {
                            return output = postRes.ToString();
                        }


                    }
                    else
                    {

                        ServiceLayer2Auto.open(sMethod, $@"{url}{sModule}", false);
                        ServiceLayer2Auto.send(sJson.ToString());
                    }

                    var response = ServiceLayer2Auto.responseText;

                    if (string.IsNullOrEmpty(sRetValue))
                    {
                        output = response.ToString();
                    }
                    else
                    {
                        output = GetJsonValue(response, sRetValue);
                    }
                }

                return output;
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }
        public async Task<string> PostSAPAsync(string sMethod, string sModule, string sJson, string sRetValue, PostingViewModel Creds, string CardCode, string IPDocEntry, bool IP_toBePosted, string OPDocEntry)

        {
            try
            {
                //PostHelper ph = new PostHelper();
                ServiceLayer2Auto = new ServerXMLHTTP60();
                ServiceLayer2Auto.setOption(SERVERXMLHTTP_OPTION.SXH_OPTION_IGNORE_SERVER_SSL_CERT_ERROR_FLAGS, 13056);

                var url = $"http://{Creds.CredentialDetails.SAPSldServer}:{Creds.CredentialDetails.SAPLicensePort}/b1s/v1/";
                const string httpStr = "http://";
                const string httpsStr = "https://";
                if (!url.StartsWith(httpStr, true, null) &&
                    !url.StartsWith(httpsStr, true, null))
                {
                    url = httpStr + url;
                }

                string output;
                var json = new StringBuilder();
                json.AppendLine("{");
                json.AppendLine($@" ""CompanyDB"" : ""{Creds.CredentialDetails.SAPDBName}"",");
                json.AppendLine($@" ""UserName"" : ""{Creds.CredentialDetails.SAPUser}"",");
                json.AppendLine($@" ""Password"" : ""{Creds.CredentialDetails.SAPPassword}""");
                json.AppendLine("}");

                ServiceLayer2Auto.open("POST", $@"{url}Login", false);
                try
                {
                    ServiceLayer2Auto.send(json.ToString());
                    var response = ServiceLayer2Auto.responseText;
                    if (response.ToLower().Contains("bad request"))
                    {
                        url = url.Replace("http", "https");
                        ServiceLayer2Auto.open("POST", $@"{url}Login", false);
                        ServiceLayer2Auto.send(json.ToString());
                    }
                }
                catch (Exception ex)
                {
                    if (ex.Message.ToLower().Contains("terminated abnormally"))
                    {
                        url = url.Replace("http", "https");
                        ServiceLayer2Auto.open("POST", $@"{url}Login", false);
                        ServiceLayer2Auto.send(json.ToString());
                    }
                    else
                    {
                        return ex.Message;
                    }
                }

                string ret = GetJsonValue(ServiceLayer2Auto.responseText, "SessionId");

                if (string.IsNullOrEmpty(ret))
                {
                    output = GetJsonError(ServiceLayer2Auto.responseText);
                }
                else
                {
                    
                  
                    ServiceLayer2Auto.open(sMethod, $@"{url}{sModule}", false);
                    ServiceLayer2Auto.send(sJson.ToString());
                    

                    var response = ServiceLayer2Auto.responseText;

                    if (string.IsNullOrEmpty(sRetValue))
                    {
                        output = response.ToString();
                    }
                    else
                    {
                        output = GetJsonValue(response, sRetValue);
                    }
                }

                return output;
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }

        public static string GetJsonValue(string json, string value)
        {
            try
            {
                if (json != null)
                {
                    JObject err = JObject.Parse(json);
                    if (err.ToString().Contains("error"))
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
                if (json.Contains("error"))
                {
                    string retJson = GetJsonString(json, "");
                    var sbJson = new StringBuilder();
                    sbJson.Append("{" + retJson + "}}}");
                    return GetJsonError(sbJson.ToString());
                }
                else { return "Operation completed successfully"; }
            }
        }
        public static string GetJsonError(string json)
        {
            JObject err = JObject.Parse(json);
            return (string)err["error"]["message"]["value"];
        }
        public static string GetJsonString(string ret, string tag)
        {
            var startTag = "{";
            int startIndex = ret.IndexOf(startTag) + startTag.Length;
            int endIndex = ret.IndexOf("}", startIndex);
            return ret.Substring(startIndex, endIndex - startIndex);
        }

        public static async Task<string> UploadToSharePoint(string FileName, string FilePath,APISetup apisetup)
        {
            try
            {
                string clientId = apisetup.APIKey;
                string tenantId = apisetup.APIToken;
                string clientSecret = apisetup.APISecretKey;
                string driveId = apisetup.APILoginBody;
                string folderName = apisetup.APILoginUrl;

                //string clientId = "5d243834-fa7f-47f8-8061-3bf2d39f95b0";//0
                //string tenantId = "4896b0d0-e0bf-45ee-a948-09a4059b72dc";
                //string clientSecret = "Zmn8Q~ZXJjWPUjVys.uPZLuWiuqqWydLmNBkjckZ";
                //string driveId = "b!XQLA0t1FZkOPsfMhypzhMapKvy69hBdLvCu44Mf5UocDiyYi6cijRalcyfE82et6";
                //string folderName = "AMC";

                string accessToken = await GetAccessToken(clientId, tenantId, clientSecret);

                if (!accessToken.Contains("error"))
                {
                    string url = $"https://graph.microsoft.com/v1.0/drives/{driveId}/root:/{folderName}/{FileName}:/content";

                    using (HttpClient client = new HttpClient())
                    {  
                        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

                        var filepath_name = $"{FilePath}\\{FileName}";
                        // Read file asynchronously
                        byte[] fileContents = await Task.Run(() => File.ReadAllBytes(filepath_name));

                        using (ByteArrayContent byteContent = new ByteArrayContent(fileContents))
                        {
                            byteContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

                            HttpResponseMessage response = await client.PutAsync(url, byteContent);

                            if (response.IsSuccessStatusCode)
                            {
                                return "File uploaded successfully.";
                            }
                            else
                            {
                                string responseContent = await response.Content.ReadAsStringAsync();
                                return $"error Failed to upload file. Status code: {response.StatusCode}, Reason: {responseContent}";
                            }
                        }
                    }
                }
                else
                {
                    return accessToken;
                }
                //string[] scopes = { "https://graph.microsoft.com/.default" };

                //var confidentialClient = ConfidentialClientApplicationBuilder
                //    .Create(clientId)
                //    .WithClientSecret(clientSecret)
                //    .WithAuthority(new Uri($"https://login.microsoftonline.com/{tenantId}"))
                //    .Build();

                //var authResult = await confidentialClient.AcquireTokenForClient(scopes).ExecuteAsync();

                //string accessToken = authResult.AccessToken;


            }
            catch (Exception e)
            {
                  return $"error Exception during file upload: {e.Message.ToString()}";
            }
           


        }


        public static async Task<string> GetAccessToken(string clientId, string tenantId, string clientSecret)
        {
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    // Define the token request URL
                    var url = $"https://login.microsoftonline.com/{tenantId}/oauth2/v2.0/token";

                    // Create the content for the POST request
                    var parameters = new FormUrlEncodedContent(new[]
                    {
                    new KeyValuePair<string, string>("grant_type", "client_credentials"),
                    new KeyValuePair<string, string>("client_id", clientId),
                    new KeyValuePair<string, string>("client_secret", clientSecret),
                    new KeyValuePair<string, string>("scope", "https://graph.microsoft.com/.default"),
                    });

                    // Send the POST request to obtain the token
                    HttpResponseMessage response = await client.PostAsync(url, parameters);

                    // Ensure successful response   
                     response.EnsureSuccessStatusCode();

                    // Read and parse the response
                    string responseBody = await response.Content.ReadAsStringAsync();
                    var json = JObject.Parse(responseBody);
                    string accessToken = json["access_token"]?.ToString();

                    if (string.IsNullOrEmpty(accessToken))
                    {
                        throw new Exception("Access token not found in the response.");
                    }

                    return accessToken;
                }
            }
            catch (Exception ex)
            {
                return $" error {ex.Message}";
            }
        }




    }
}