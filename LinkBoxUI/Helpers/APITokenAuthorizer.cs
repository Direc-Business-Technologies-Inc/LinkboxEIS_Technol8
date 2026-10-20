using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;

namespace LinkBoxUI.Helpers
{
    public static class APITokenAuthorizer
    {
        public static string GetToken(string method, string url, string appKey, string subsKey)
        {
            string result = "";
            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                HttpWebResponse httpResponse;
                request.AutomaticDecompression = DecompressionMethods.GZip;
                request.Method = string.IsNullOrEmpty(method) ? "POST" : method;
                request.Accept = "*/*";
                request.ContentType = "application/json";
                request.Headers.Add("OC-Api-App-Key", appKey);
                request.Headers.Add("Ocp-Apim-Subscription-Key", subsKey);

                httpResponse = (HttpWebResponse)request.GetResponse();
                using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
                {
                    var jObj = (JObject)JsonConvert.DeserializeObject(streamReader.ReadToEnd());
                    result = jObj["token"].ToString();
                }
            }
            catch (Exception exc)
            {
                Console.WriteLine(exc.ToString());
            }

            return result;
        }

        public static string GetAPIFields(string method, string url, string appKey, string subsKey, string token, int compId, string insType)
        {
            //string url = "https://kationtech-moo.azurewebsites.net/api/customer";
            string result = "";
            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                HttpWebResponse httpResponse;
                request.AutomaticDecompression = DecompressionMethods.GZip;
                request.Method = string.IsNullOrEmpty(method) ? "POST" : method;
                request.Accept = "*/*";
                request.ContentType = "application/json";
                request.Headers.Add("OC-Api-App-Key", appKey);
                request.Headers.Add("Ocp-Apim-Subscription-Key", subsKey);
                request.Headers.Add("Company-Id", compId.ToString());
                request.Headers.Add("Authorization", "Bearer " + token);
                request.Headers.Add("Instance-Type", insType);

                httpResponse = (HttpWebResponse)request.GetResponse();
                using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
                {
                    result = streamReader.ReadToEnd();
                }
            }
            catch (WebException webex)
            {
                string exresult;
                using (var reader = new StreamReader(webex.Response.GetResponseStream()))
                {
                    exresult = reader.ReadToEnd();
                }
                return $@"error: {exresult}";
            }

            return result;
        }

        public static string HTTPWebRequest(string method, string url, string jsonB,string appKey, string subsKey, string token, string insType)
        {
            string res = "";
            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                HttpWebResponse httpResponse;
                request.AutomaticDecompression = DecompressionMethods.GZip;
                request.Method = string.IsNullOrEmpty(method) ? "POST" : method;
                request.Accept = "*/*";
                request.ContentType = "application/json";
                request.Headers.Add("OC-Api-App-Key", appKey);
                request.Headers.Add("Ocp-Apim-Subscription-Key", subsKey);
                request.Headers.Add("Authorization", "Bearer " + token);
                request.Headers.Add("Instance-Type", insType);
                if (method.ToLower().Contains("post") || method.ToLower().Contains("put"))
                {
                    var body = Encoding.ASCII.GetBytes(jsonB);
                    request.ContentLength = body.Length;
                    using (var streamwriter = request.GetRequestStream())
                    {
                        streamwriter.Write(body, 0, body.Length);
                        streamwriter.Flush();
                    }
                }

                httpResponse = (HttpWebResponse)request.GetResponse();
                using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
                {
                    res = streamReader.ReadToEnd();
                }
            }
            catch (WebException webex)
            {
                string exresult;
                using (var reader = new StreamReader(webex.Response.GetResponseStream()))
                {
                    exresult = reader.ReadToEnd();
                }
                return $@"error: {exresult}";
            }


            return res;
        }

        public static string ConvertHtmlToPlainText(string html)
        {
            if (string.IsNullOrWhiteSpace(html))
                return string.Empty;

            // Replace common block tags with line breaks first
            html = Regex.Replace(html, @"</p\s*>", "\n", RegexOptions.IgnoreCase);
            html = Regex.Replace(html, @"<br\s*/?>", "\n", RegexOptions.IgnoreCase);

            // Remove all remaining HTML tags
            html = Regex.Replace(html, "<.*?>", string.Empty);

            // Decode HTML entities like &quot;
            html = WebUtility.HtmlDecode(html);

            // Clean extra blank lines/spaces
            html = Regex.Replace(html, @"\n{2,}", "\n\n").Trim();

            return html;
        }

    }
}