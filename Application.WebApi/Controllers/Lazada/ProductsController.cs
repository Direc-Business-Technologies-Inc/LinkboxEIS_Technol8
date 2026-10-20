using System;
using System.Collections.Generic;
using System.Configuration;
using System.Security.Cryptography;
using System.Text;
using System.Net.Http;
using System.Threading.Tasks;
using Application.WebApi.Service.Lazada;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace Application.WebApi.Controllers.Lazada
{
    [ApiController]
    [Route("api/lazada/products")]
    public class ProductsController : ControllerBase
    {
        private readonly LazadaClient _lazadaClient;
        private readonly LazadaAuthService _authService;
        private readonly IConfiguration _config;

        public ProductsController(LazadaClient lazadaClient, LazadaAuthService authService, IConfiguration config)
        {
            _lazadaClient = lazadaClient;
            _authService = authService;
            _config = config;
        }

        // GET /api/lazada/products
        [HttpGet]
        public async Task<IActionResult> GetProducts(
            [FromQuery] string filter = "live",
            [FromQuery] int limit = 10,
            [FromQuery] int offset = 0)
        {
            try
            {
                var accessToken = await _authService.GetValidAccessTokenAsync();
                var result = await _lazadaClient.GetProductsAsync(accessToken, filter, limit);
                return Ok(result.RootElement);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = "error", message = ex.Message });
            }
        }

        // ← TEST - GET /api/lazada/products/test-image?fileId=30848
        [HttpGet("test-image")]
        public async Task<IActionResult> TestImage([FromQuery] string fileId)
        {
            try
            {
                var accountId = _config["accountId"];
                var consumerKey = _config["consumerKey"];
                var consumerSecret = _config["consumerSecret"];
                var tokenId = _config["tokenId"];
                var tokenSecret = _config["tokenSecret"];

                string baseUrl = $"https://{accountId}.restlets.api.netsuite.com/app/site/hosting/restlet.nl";

                var queryParams = new Dictionary<string, string>
                {
                    { "script", "671" },
                    { "deploy", "1"   },
                    { "fileId", fileId }
                };

                var oauthParams = new Dictionary<string, string>
                {
                    { "oauth_consumer_key",     consumerKey },
                    { "oauth_token",            tokenId },
                    { "oauth_nonce",            Guid.NewGuid().ToString("N") },
                    { "oauth_timestamp",        ((int)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds).ToString() },
                    { "oauth_signature_method", "HMAC-SHA256" },
                    { "oauth_version",          "1.0" }
                };

                var allParams = new Dictionary<string, string>();
                foreach (var kv in queryParams) allParams.Add(kv.Key, kv.Value);
                foreach (var kv in oauthParams) allParams.Add(kv.Key, kv.Value);

                var paramList = new List<string>();
                foreach (var kv in allParams)
                    paramList.Add($"{UrlEncode(kv.Key)}={UrlEncode(kv.Value)}");
                paramList.Sort();
                string normalizedParams = string.Join("&", paramList);

                string signatureBase = $"GET&{UrlEncode(baseUrl)}&{UrlEncode(normalizedParams)}";
                string signingKey = $"{UrlEncode(consumerSecret)}&{UrlEncode(tokenSecret)}";

                string signature;
                using (var hmac = new HMACSHA256(Encoding.ASCII.GetBytes(signingKey)))
                    signature = Convert.ToBase64String(hmac.ComputeHash(Encoding.ASCII.GetBytes(signatureBase)));

                var authHeader = new StringBuilder();
                authHeader.Append($"OAuth realm=\"{accountId}\",");
                foreach (var kv in oauthParams)
                    authHeader.Append($"{kv.Key}=\"{UrlEncode(kv.Value)}\",");
                authHeader.Append($"oauth_signature=\"{UrlEncode(signature)}\"");

                var requestUrl = baseUrl + "?" + string.Join("&",
                    new List<string>
                    {
                        $"script={UrlEncode("671")}",
                        $"deploy={UrlEncode("1")}",
                        $"fileId={UrlEncode(fileId)}"
                    });

                using (var client = new HttpClient())
                using (var request = new HttpRequestMessage(HttpMethod.Get, requestUrl))
                {
                    request.Headers.Add("Authorization", authHeader.ToString());
                    request.Headers.Add("Accept", "application/json");

                    var response = await client.SendAsync(request);
                    var body = await response.Content.ReadAsStringAsync();

                    if (!response.IsSuccessStatusCode)
                        return StatusCode((int)response.StatusCode, body);

                    return Ok(JsonConvert.DeserializeObject(body));
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = "error", message = ex.Message });
            }
        }

        private static string UrlEncode(string value)
        {
            return Uri.EscapeDataString(value ?? string.Empty)
                .Replace("!", "%21")
                .Replace("*", "%2A")
                .Replace("'", "%27")
                .Replace("(", "%28")
                .Replace(")", "%29");
        }
    }
}
