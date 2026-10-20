using Application.WebApi.Models;
using Application.WebApi.Service;
using Azure.Core;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using static Application.WebApi.Models.NetsuiteItemModel;
namespace Application.WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ZaloraController : ControllerBase
    {

        private readonly ZaloraImageService _imageService;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;

        // Inject IHttpClientFactory and IConfiguration into the constructor
        public ZaloraController(IHttpClientFactory httpClientFactory, IConfiguration configuration, ZaloraImageService imageService)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _imageService = imageService;
        }

        [HttpGet("images")]
        public async Task<IActionResult> GetImages([FromQuery] string sku, [FromQuery] string[] internalIds)
        {
            if (string.IsNullOrWhiteSpace(sku))
                return BadRequest(new { error = "sku is required" });

            if (internalIds == null || internalIds.Length == 0)
                return BadRequest(new { error = "internalIds is required" });

            var items = new List<ImageItemDto>();

            foreach (var internalId in internalIds.Where(x => !string.IsNullOrWhiteSpace(x)))
            {
                var result = await GetImageDetails(internalId); // your existing method

                if (result is OkObjectResult ok && ok.Value is ImageDetailsResponse imageDetails &&
                    !string.IsNullOrWhiteSpace(imageDetails.base64) &&
                    !string.IsNullOrWhiteSpace(imageDetails.name))
                {
                    // ✅ Save under Images/<sku>/
                    var savedFileName = await _imageService.PostImage(
                        imageDetails.base64,
                        imageDetails.name,
                        internalId,
                        sku
                    );

                    // ✅ URL must match the GET image route below
                    var displayUrl =
                        $"{Request.Scheme}://{Request.Host}/api/Zalora/image/" +
                        $"{Uri.EscapeDataString(sku)}/{Uri.EscapeDataString(savedFileName)}";

                    //items.Add(new { internalId, savedFileName, displayUrl});
                    items.Add(new ImageItemDto
                    {
                        InternalId = internalId,
                        SavedFileName = savedFileName,
                        DisplayUrl = displayUrl
                    });
                }
                else
                {
                   // items.Add(new { internalId, displayUrl = (string?)null, error = "GetImageDetails failed/invalid payload" });
                    items.Add(new ImageItemDto
                    {
                        InternalId = internalId,
                        Error = "GetImageDetails failed/invalid payload"
                    });
                }
            }

            return Ok(new GetImagesResponse
            {
                Sku = sku,
                Items = items
            });
        }
        [HttpGet("image/{sku}/{fileName}")]
        public IActionResult ViewImage(string sku, string fileName)
        {
            return _imageService.GetImageFromFolder(sku, fileName);
        }

        //[HttpGet("image/{fileName}")]
        //public IActionResult GetImageFromFolder(string fileName)
        //{
        //    // Prevent path traversal
        //    fileName = Path.GetFileName(fileName);

        //    var imagesPath = Path.Combine(AppContext.BaseDirectory, "Images");
        //    var fullPath = Path.Combine(imagesPath, fileName);

        //    if (!System.IO.File.Exists(fullPath))
        //        return NotFound(new { message = $"File not found: {fileName}" });

        //    // Determine content type (jpg/png/webp/etc.)
        //    var provider = new FileExtensionContentTypeProvider();
        //    if (!provider.TryGetContentType(fullPath, out var contentType))
        //        contentType = "application/octet-stream";

        //    return PhysicalFile(fullPath, contentType);
        //}

        //[HttpGet("image-url/{fileName}")]
        //public IActionResult GetImageDisplayUrl(string fileName)
        //{
        //    fileName = Path.GetFileName(fileName);

        //    // This builds: https://host/api/Zalora/image/<fileName>
        //    var displayUrl = $"{Request.Scheme}://{Request.Host}/api/Zalora/image/{Uri.EscapeDataString(fileName)}";

        //    return Ok(new { displayUrl });
        //}


        [HttpGet("GetImageDetails")]
        public async Task<IActionResult> GetImageDetails(string FileId)
        {
            try
            {
                // ===== Load from configuration properly =====
                string accountId = _configuration["NetSuite:AccountId"];
                string consumerKey = _configuration["NetSuite:ConsumerKey"];
                string consumerSecret = _configuration["NetSuite:ConsumerSecret"];
                string tokenId = _configuration["NetSuite:TokenId"];
                string tokenSecret = _configuration["NetSuite:TokenSecret"];

                // ✅ Validate config early
                if (string.IsNullOrWhiteSpace(accountId) ||
                    string.IsNullOrWhiteSpace(consumerKey) ||
                    string.IsNullOrWhiteSpace(consumerSecret) ||
                    string.IsNullOrWhiteSpace(tokenId) ||
                    string.IsNullOrWhiteSpace(tokenSecret))
                {
                    return StatusCode(500, "NetSuite configuration is missing.");
                }

                string baseUrl =
                    $"https://{accountId}.restlets.api.netsuite.com/app/site/hosting/restlet.nl";

                // ===== Query Parameters =====
                var queryParams = new Dictionary<string, string>
        {
            { "script", "671" },
            { "deploy", "1" },
            { "fileId", FileId }

                   
        };

                // ===== OAuth Parameters =====
                var oauthParams = new Dictionary<string, string>
        {
            { "oauth_consumer_key", consumerKey },
            { "oauth_token", tokenId },
            { "oauth_nonce", Guid.NewGuid().ToString("N") },
            { "oauth_timestamp",
                ((int)(DateTime.UtcNow -
                new DateTime(1970, 1, 1)).TotalSeconds).ToString()
            },
            { "oauth_signature_method", "HMAC-SHA256" },
            { "oauth_version", "1.0" }
        };

                // Combine parameters
                var allParams = queryParams
                    .Concat(oauthParams)
                    .ToDictionary(k => k.Key, v => v.Value);

                // Normalize
                var normalizedParams = string.Join("&",
                    allParams
                        .OrderBy(k => k.Key)
                        .Select(k => $"{Uri.EscapeDataString(k.Key)}={Uri.EscapeDataString(k.Value)}"));

                string signatureBase =
                    "GET&" +
                    Uri.EscapeDataString(baseUrl) + "&" +
                    Uri.EscapeDataString(normalizedParams);

                string signingKey =
                    Uri.EscapeDataString(consumerSecret) + "&" +
                    Uri.EscapeDataString(tokenSecret);

                string signature;
                using (var hmac =
                    new HMACSHA256(Encoding.ASCII.GetBytes(signingKey)))
                {
                    signature = Convert.ToBase64String(
                        hmac.ComputeHash(Encoding.ASCII.GetBytes(signatureBase))
                    );
                }

                // Authorization header
                var authHeader = new StringBuilder();
                authHeader.Append($"OAuth realm=\"{accountId}\",");

                foreach (var kv in oauthParams)
                {
                    authHeader.Append(
                        $"{kv.Key}=\"{Uri.EscapeDataString(kv.Value)}\",");
                }

                authHeader.Append($"oauth_signature=\"{Uri.EscapeDataString(signature)}\"");

                // Build request URL
                string requestUrl =
                    baseUrl + "?" +
                    string.Join("&",
                        queryParams.Select(k =>
                            $"{k.Key}={Uri.EscapeDataString(k.Value)}"));

                var client = _httpClientFactory.CreateClient();

                var request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
                request.Headers.Add("Authorization", authHeader.ToString());
                request.Headers.Add("Accept", "application/json");

                var response = await client.SendAsync(request);
                var body = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    return StatusCode((int)response.StatusCode, body);
                }

                var obj = JsonConvert.DeserializeObject<ImageDetailsResponse>(body);

                if (obj == null)
                {
                    return StatusCode(502, "Invalid NetSuite response format.");
                }

                return Ok(obj);
            }
            catch (Exception ex)
            {
                // ✅ This prevents Swagger from crashing
                return StatusCode(500, ex.ToString());
            }
        }

       
    }

    public class ImageDetailsResponse
    {
        public bool success { get; set; }
        public int fileId { get; set; }
        public string name { get; set; }
        public string mimeType { get; set; }
        public string base64 { get; set; }
    }
}
