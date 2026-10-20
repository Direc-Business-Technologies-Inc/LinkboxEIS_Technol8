using System.Security.Cryptography;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Text.Json;
using Application.WebApi.Models;
using System.Text;
using System.Data.SqlClient;
using Microsoft.Data.SqlClient;
using Microsoft.Identity.Client;
using System.Text.RegularExpressions;
namespace Application.WebApi.Service
{
    public class NetsuiteService
    {

        private readonly string AccountId;
        private readonly string ClientCredentialsCertificateId;
        private readonly string ApiConsumerKey;
        private readonly string _privateKeyPath;
        private readonly string PrivateKeyPem;
        // private readonly HttpClient _httpClient;

        private readonly string RestApiRoot;
        private readonly string Oauth2ApiRoot;
        private readonly string RecordApiRoot;
        private readonly string SuiteQLRoot;
        private readonly string TokenEndPointUrl;

        public NetsuiteService(IConfiguration configuration)
        {
            AccountId = configuration["NetSuite:AccountId"];
            ClientCredentialsCertificateId = configuration["NetSuite:ClientCredentialsCertificateId"];
            ApiConsumerKey = configuration["NetSuite:ApiConsumerKey"];
            _privateKeyPath = configuration["NetSuite:PrivateKeyPath"];

            if (File.Exists(_privateKeyPath))
            {
                PrivateKeyPem = File.ReadAllText(_privateKeyPath, Encoding.UTF8).Trim();

            }
            else
            {
                throw new FileNotFoundException($"Private key file not found at {_privateKeyPath}");
            }

              RestApiRoot = $"https://{AccountId}.suitetalk.api.netsuite.com/services/rest";

              Oauth2ApiRoot = $"{RestApiRoot}/auth/oauth2/v1";
              RecordApiRoot = $"{RestApiRoot}/record/v1";

              SuiteQLRoot = $"{RestApiRoot}/query/v1/suiteql";

                

              TokenEndPointUrl = $"{Oauth2ApiRoot}/token";
        // _httpClient = httpClient;
        }


        //        private const string AccountId = "9707602-sb1";

        //        private const string ClientCredentialsCertificateId = "LXf0TW3k3TsYhGh2VKEu5cXtvbws5TUgLbHWHE9kwz8";
        //        private const string ApiConsumerKey = "3375b3ac9ee8c61606696c192ef4f7107244fdb1c5ee55d7bf2f8fc966e735ef";

        //        private const string PrivateKeyPem = @"-----BEGIN PRIVATE KEY-----
        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
        //bEZVBHEH/6c3IjkZjviFtDw=
        //-----END PRIVATE KEY-----";


        //private string RestApiRoot = $"https://{AccountId}.suitetalk.api.netsuite.com/services/rest";

        //private static string Oauth2ApiRoot = $"{RestApiRoot}/auth/oauth2/v1";
        //private static string RecordApiRoot = $"{RestApiRoot}/record/v1";

        //private static string SuiteQLRoot = $"{RestApiRoot}/query/v1/suiteql";

        //private static string TokenEndPointUrl = $"{Oauth2ApiRoot}/token";

        private static readonly HttpClient _httpClient = new HttpClient();

        private static string _accessToken;
        private static DateTime _tokenExpiryTime;

        private static DateTime _jwtTokenExpiryTime = DateTime.MinValue;
        private string tokenText = "";

        public async Task<string> GetAccessToken()
        {

            var url = Oauth2ApiRoot + "/token/";
            string clientAssertion = GetJwtToken();
            _tokenExpiryTime = DateTime.Now.AddMinutes(30);
            var requestParams = new List<KeyValuePair<string, string>>()
        {
            new KeyValuePair<string, string>("grant_type", "client_credentials"),
            new KeyValuePair<string, string>("client_assertion_type", "urn:ietf:params:oauth:client-assertion-type:jwt-bearer"),
            new KeyValuePair<string, string>("client_assertion", clientAssertion)
        };

            int maxtry = 3;
            for (int i = 0; i < maxtry; i++)
            {
                using var httpRequest = new HttpRequestMessage(HttpMethod.Post, url);

                httpRequest.Content = new FormUrlEncodedContent(requestParams);
                
                var httpResponse = await _httpClient.SendAsync(httpRequest);

                if (httpResponse.IsSuccessStatusCode)
                {
                    var responseJson = await httpResponse.Content.ReadAsStringAsync();
                    var response = System.Text.Json.JsonSerializer.Deserialize<NetsuiteToken>(responseJson);
                    return response?.access_token ?? throw new Exception("Error :  Access token not found in response.");
                }
                else
                {
                    var errorContent = await httpResponse.Content.ReadAsStringAsync();
                    if (i < maxtry - 1) // Avoid delaying after the last attempt
                    {
                        // Optional: Exponential backoff delay
                        //int delay = (int)Math.Pow(2, i) * 1000; // 1s, 2s, 4s...
                        await Task.Delay(5000);
                        continue;
                    }
                    // Log the full error response
                    throw new Exception($"Error retrieving access token: {httpResponse.StatusCode} - {errorContent}");
                }
            }

            //var responseJson = await httpResponse.Content.ReadAsStringAsync();
            ////var response = JsonSerializer.Deserialize<NetSuiteToken>(responseJson);
            //var response = System.Text.Json.JsonSerializer.Deserialize<NetSuiteToken>(responseJson);
            //return response.access_token;
            throw new Exception("Access token not found in response.");
        }
        private static string FixBase64Padding(string base64)
        {
            int mod4 = base64.Length % 4;
            if (mod4 > 0)
            {
                base64 += new string('=', 4 - mod4); // Append '=' padding if needed
            }
            return base64;
        }
        private static RSA _rsa;

        //private string GetJwtToken()
        //{
        //    if (_rsa == null)
        //    {
        //        _rsa = RSA.Create();
        //    }

        //    string privateKeyPem = PrivateKeyPem;

        //    // Remove headers and footers
        //    privateKeyPem = privateKeyPem.Replace("-----BEGIN PRIVATE KEY-----", "")
        //                                 .Replace("-----END PRIVATE KEY-----", "")
        //                                 .Replace("\n", "")
        //                                 .Replace("\r", "")
        //                                 .Trim();

        //    privateKeyPem = Regex.Replace(privateKeyPem, @"[^A-Za-z0-9+/=]", "");
        //    privateKeyPem = FixBase64Padding(privateKeyPem);
        //    // Decode the Base64 key
        //    byte[] privateKeyRaw = Convert.FromBase64String(privateKeyPem);

        //    _rsa.ImportPkcs8PrivateKey(privateKeyRaw, out _);

        //    // Ensure the key size is at least 3072 bits
        //    if (_rsa.KeySize < 3072)
        //    {
        //        throw new Exception("RSA key size must be at least 3072 bits for NetSuite OAuth.");
        //    }

        //    // Use RSA-PSS (PS256) instead of RS256
        //    var rsaSecurityKey = new RsaSecurityKey(_rsa)
        //    {
        //        KeyId = ClientCredentialsCertificateId
        //    };

        //    var signingCreds = new SigningCredentials(rsaSecurityKey, SecurityAlgorithms.RsaSsaPssSha256);

        //    var now = DateTime.UtcNow;

        //    var tokenHandler = new JwtSecurityTokenHandler();
        //    var tokenDescriptor = new SecurityTokenDescriptor
        //    {
        //        Issuer = ApiConsumerKey,
        //        Audience = TokenEndPointUrl,
        //        Expires = now.AddMinutes(10),
        //        IssuedAt = now,
        //        Claims = new Dictionary<string, object>
        //{
        //    { "scope", new[] { "rest_webservices" } }
        //},
        //        SigningCredentials = signingCreds
        //    };

        //    var token = tokenHandler.CreateToken(tokenDescriptor);
        //    return tokenHandler.WriteToken(token);
        //}

        private string GetJwtToken()
        {

            if (!string.IsNullOrEmpty(tokenText) && DateTime.Now < _jwtTokenExpiryTime)
            {
                return tokenText; // Return the existing token if it's still valid.
            }
            string privateKeyPem = PrivateKeyPem;
            //Remove headers and footers
                privateKeyPem = privateKeyPem.Replace("-----BEGIN PRIVATE KEY-----", "")
                                             .Replace("-----END PRIVATE KEY-----", "")
                                             .Replace("\n", "")
                                             .Replace("\r", "")
                                             .Trim();

            privateKeyPem = Regex.Replace(privateKeyPem, @"[^A-Za-z0-9+/=]", "");
            privateKeyPem = FixBase64Padding(privateKeyPem);
            // Decode the Base64 key
            byte[] privateKeyRaw = Convert.FromBase64String(privateKeyPem);
            var rsa = RSACng.Create();
            rsa.KeySize = 3072;
            rsa.ImportPkcs8PrivateKey(privateKeyRaw, out _);
            // Ensure the key size is at least 3072 bits
            if (rsa.KeySize < 3072)
            {
                throw new Exception("RSA key size must be at least 3072 bits for NetSuite OAuth.");
            }
            // Use RSA-PSS (PS256) instead of RS256
            var rsaSecurityKey = new RsaSecurityKey(rsa)
            {
                KeyId = ClientCredentialsCertificateId
            };
            var signingCreds = new SigningCredentials(rsaSecurityKey, SecurityAlgorithms.RsaSsaPssSha256);
            var now = DateTime.UtcNow;
            var tokenHandler = new JwtSecurityTokenHandler { SetDefaultTimesOnTokenCreation = false };
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Issuer = ApiConsumerKey,
                Audience = TokenEndPointUrl,
                Expires = now.AddMinutes(10),
                IssuedAt = now,
                Claims = new Dictionary<string, object>
                {
                 { "scope", new[] { "rest_webservices" } }
                 },
                SigningCredentials = signingCreds
            };
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }
        public async Task<(bool IsSuccess, string ResponseMessage)> NetsuitePostData(string data,string module, string method, string id = null)
        {
            try
            {
                // Ensure you have the access token
                if (_accessToken == null || DateTime.Now >= _tokenExpiryTime)
                {
                    _accessToken = await GetAccessToken();
                }
                var isPost = method.Equals("POST", StringComparison.OrdinalIgnoreCase);
                var isPatch = method.Equals("PATCH", StringComparison.OrdinalIgnoreCase);

                if (!isPost && !isPatch)
                    return (false, $"Unsupported HTTP method: {method}");

                if (isPatch && string.IsNullOrWhiteSpace(id))
                    return (false, "PATCH requires a record ID.");

                var url = isPatch
                    ? $"{RecordApiRoot}/{module}/{id}"
                    : $"{RecordApiRoot}/{module}";

                var httpMethod = isPost ? HttpMethod.Post : HttpMethod.Patch;

                using var httpRequest = new HttpRequestMessage(httpMethod, url);
                httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);
                httpRequest.Content = new StringContent(data, Encoding.UTF8, "application/json");

                // Send the request and handle the response
                var httpResponse = await _httpClient.SendAsync(httpRequest);
                var responseJson = await httpResponse.Content.ReadAsStringAsync();

                if (!httpResponse.IsSuccessStatusCode)
                {
                    // Return false with the detailed error message
                    return (false, $"Failed to Transaction: Status: {httpResponse.StatusCode}, Response: {responseJson}");
                }

                //return (true, httpResponse.StatusCode.ToString());
                return (true, responseJson); // Return success with response message
            }
            catch (Exception ex)
            {
                return (false, $"Error Posting transactions: {ex.Message}");
            }
        }


        public async Task<Response> QueryService(string query)
        {
            try
            {
                var url = SuiteQLRoot;

                // Refresh access token if null or expired
                if (_accessToken == null || DateTime.Now >= _tokenExpiryTime)
                    _accessToken = await GetAccessToken();

                using var httpRequest = new HttpRequestMessage(HttpMethod.Post, url);

                // Add headers
                httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);
                httpRequest.Headers.Add("Prefer", "transient");

                // Create JSON body
                var jsonBody = new { q = query };
                string jsonString = System.Text.Json.JsonSerializer.Serialize(jsonBody);

                // Add content
                httpRequest.Content = new StringContent(jsonString, Encoding.UTF8, "application/json");

                // Send HTTP request
                var httpResponse = await _httpClient.SendAsync(httpRequest);
                var responseJson = await httpResponse.Content.ReadAsStringAsync();

                if (httpResponse.IsSuccessStatusCode)
                {
                    // Deserialize JSON response into an object
                    var apiResponse = System.Text.Json.JsonSerializer.Deserialize<NetsuiteApiResponse>(responseJson);

                    // Return success response
                    return new Response
                    {
                        Error = false,
                        Message = "Query executed successfully.",
                        Data = apiResponse
                    };
                }
                else
                {
                    // Log the error content
                    var errorContent = await httpResponse.Content.ReadAsStringAsync();

                    // Return error response
                    return new Response
                    {
                        Error = true,
                        Message = "Failed to execute query.",
                        ResponseError = $"Status Code: {httpResponse.StatusCode}, Error: {errorContent}"
                    };
                }
            }
            catch (Exception ex)
            {
                // Return exception details in response
                return new Response
                {
                    Error = true,
                    Message = "An error occurred while executing the query.",
                    ResponseError = ex.ToString()
                };
            }
        }

        public async Task<(bool IsSuccess, string ResponseMessage)> DeleteSalesOrder(string id)
        {
            try
            {
                if (_accessToken == null || DateTime.Now >= _tokenExpiryTime)
                    _accessToken = await GetAccessToken();

                var url = $"{RecordApiRoot}/salesorder/{id}";

                using var httpRequest = new HttpRequestMessage(HttpMethod.Delete, url);
                httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);

                var httpResponse = await _httpClient.SendAsync(httpRequest);
                var responseJson = await httpResponse.Content.ReadAsStringAsync();

                if (httpResponse.IsSuccessStatusCode)
                    return (true, $"Sales Order {id} successfully deleted!");
                else
                    return (false, $"Failed to delete: {httpResponse.StatusCode} - {responseJson}");
            }
            catch (Exception ex)
            {
                return (false, $"Error deleting Sales Order: {ex.Message}");
            }
        }

        public async Task<Response> ItemQueryService(string query)
        {
            try
            {
                var url = SuiteQLRoot;

                // Refresh access token if null or expired
                if (_accessToken == null || DateTime.Now >= _tokenExpiryTime)
                    _accessToken = await GetAccessToken();

                using var httpRequest = new HttpRequestMessage(HttpMethod.Post, url);

                // Add headers
                httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);
                httpRequest.Headers.Add("Prefer", "transient");

                // Create JSON body
                var jsonBody = new { q = query };
                string jsonString = System.Text.Json.JsonSerializer.Serialize(jsonBody);

                // Add content
                httpRequest.Content = new StringContent(jsonString, Encoding.UTF8, "application/json");

                // Send HTTP request
                var httpResponse = await _httpClient.SendAsync(httpRequest);
                var responseJson = await httpResponse.Content.ReadAsStringAsync();

                //using var doc = JsonDocument.Parse(responseJson);

                //int count = doc.RootElement.GetProperty("count").GetInt32();

                //&& count > 0
                if (httpResponse.IsSuccessStatusCode)
                {
                    // Deserialize JSON response into an object
                    var apiResponse = System.Text.Json.JsonSerializer.Deserialize<NetsuiteItemModel>(responseJson);

                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                      
                    // Return success response
                    return new Response
                    {
                        Error = false,
                        Message = "Query executed successfully.",
                        Data = apiResponse
                    };


                }
                else
                {
                    // Log the error content
                    var errorContent = await httpResponse.Content.ReadAsStringAsync();

                    // Return error response
                    return new Response
                    {
                        Error = true,
                        Message = "Failed to execute query.",
                        ResponseError = $"Status Code: {httpResponse.StatusCode}, Error: {errorContent}"
                    };
                }
            }
            catch (Exception ex)
            {
                // Return exception details in response
                return new Response
                {
                    Error = true,
                    Message = "An error occurred while executing the query.",
                    ResponseError = ex.ToString()
                };
            }
        }


        public async Task<Response> ColumnsQueryService(string query)
        {
            try
            {
                var url = SuiteQLRoot;

                // Refresh access token if null or expired
                if (_accessToken == null || DateTime.Now >= _tokenExpiryTime)
                    _accessToken = await GetAccessToken();

                using var httpRequest = new HttpRequestMessage(HttpMethod.Post, url);

                // Add headers
                httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);
                httpRequest.Headers.Add("Prefer", "transient");

                // Create JSON body
                var jsonBody = new { q = query };
                string jsonString = System.Text.Json.JsonSerializer.Serialize(jsonBody);

                // Add content
                httpRequest.Content = new StringContent(jsonString, Encoding.UTF8, "application/json");

                // Send HTTP request
                var httpResponse = await _httpClient.SendAsync(httpRequest);
                var responseJson = await httpResponse.Content.ReadAsStringAsync();

                if (httpResponse.IsSuccessStatusCode)
                {
                    // Deserialize JSON response into a dynamic object
                    using var doc = JsonDocument.Parse(responseJson);
                    var root = doc.RootElement;

                    // Extract columns (Assuming structure: { "columns": [ "Col1", "Col2", ... ] })
                    if (root.TryGetProperty("items", out JsonElement itemsElement) && itemsElement.ValueKind == JsonValueKind.Array)
            {
                var firstItem = itemsElement.EnumerateArray().FirstOrDefault();
                if (firstItem.ValueKind == JsonValueKind.Object)
                {
                    var columnNames = firstItem.EnumerateObject()
                                               .Select(prop => prop.Name)
                                               .ToArray();

                    return new Response
                    {
                        Error = false,
                        Message = "Query executed successfully.",
                        Data = columnNames // Returning only column names
                    };
                }
            }

                    return new Response
                    {
                        Error = true,
                        Message = "Columns not found in the response.",
                        ResponseError = responseJson
                    };
                }
                else
                {
                    // Log the error content
                    var errorContent = await httpResponse.Content.ReadAsStringAsync();

                    return new Response
                    {
                        Error = true,
                        Message = "Failed to execute query.",
                        ResponseError = $"Status Code: {httpResponse.StatusCode}, Error: {errorContent}"
                    };
                }
            }
            catch (Exception ex)
            {
                return new Response
                {
                    Error = true,
                    Message = "An error occurred while executing the query.",
                    ResponseError = ex.ToString()
                };
            }
        }

        public async Task<Response> GetColumnsNetsuite(string module, string id, string expand)
        {
            try
            {
                var url = RecordApiRoot + $"/{module}/{id}";
                if (bool.Parse(expand))
                {
                    url += $"?expandSubResources=true";
                }

                // Refresh access token if null or expired
                if (_accessToken == null || DateTime.Now >= _tokenExpiryTime)
                    _accessToken = await GetAccessToken();

                using var httpRequest = new HttpRequestMessage(HttpMethod.Get, url);

                // Add headers
                httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);
                httpRequest.Headers.Add("Prefer", "transient");

                // Send HTTP request
                var httpResponse = await _httpClient.SendAsync(httpRequest);
                var responseJson = await httpResponse.Content.ReadAsStringAsync();

                if (httpResponse.IsSuccessStatusCode)
                {
                    // Deserialize JSON response into a dynamic object
                    using var doc = JsonDocument.Parse(responseJson);
                    var root = doc.RootElement;

                    // To store unique first-level column names
                    var firstLevelColumns = new HashSet<string>();

                    // Iterate over all properties at the root level
                    foreach (var prop in root.EnumerateObject())
                    {
                        // If the value is a simple type (string, number, etc.), add the property name
                        if (prop.Value.ValueKind != JsonValueKind.Object && prop.Value.ValueKind != JsonValueKind.Array)
                        {
                            firstLevelColumns.Add(prop.Name);
                        }
                        else
                        {
                            // Add the property name if it's an object or array (do not go deeper)
                            firstLevelColumns.Add(prop.Name);
                        }
                    }

                    // Convert the HashSet to an array or list
                    var columnNames = firstLevelColumns.ToArray();

                    return new Response
                    {
                        Error = false,
                        Message = "Query executed successfully.",
                        Data = columnNames // Return only first-level columns as an array
                    };
                }
                else
                {
                    // Log the error content
                    var errorContent = await httpResponse.Content.ReadAsStringAsync();

                    return new Response
                    {
                        Error = true,
                        Message = "Failed to execute query.",
                        ResponseError = $"Status Code: {httpResponse.StatusCode}, Error: {errorContent}"
                    };
                }
            }
            catch (Exception ex)
            {
                return new Response
                {
                    Error = true,
                    Message = "An error occurred while executing the query.",
                    ResponseError = ex.ToString()
                };
            }
        }
    }
}