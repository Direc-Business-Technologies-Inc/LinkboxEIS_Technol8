using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace UploadFTP
{
    class APIAccess
    {
        public class ResponseMetadata
        {
            public bool IsSuccess { get; set; }

        }

        public static async Task RunAsync()
        {
            try
            {
                using (var client = new HttpClient())
                {
                    // This code sets the base URI for HTTP requests, 
                    // and sets the Accept header to "application/json", 
                    // which tells the server to send data in JSON format.
                    client.BaseAddress = new Uri("http://localhost:8004/");
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    var datatobeSent = new ResponseMetadata()
                    {
                        IsSuccess = true,
                    };
                    HttpResponseMessage response = await client.PostAsJsonAsync("api/sap/post-ordr-ip", datatobeSent);
                    response.EnsureSuccessStatusCode();
                    if (response.IsSuccessStatusCode)
                    {
                        // Get the URI of the created resource.
                        Uri ncrUrl = response.Headers.Location;
                        var b = response.Content.ReadAsStringAsync();
                        Console.WriteLine(b.Result.ToString());
                        // do whatever you need to do here with the returned data //


                    }
                }
            }
            catch (Exception ex)
            {
                string x = ex.ToString();
            }

        }
    }
}