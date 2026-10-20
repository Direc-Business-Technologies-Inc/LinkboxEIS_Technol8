using Antlr.Runtime;
using LinkBoxUI.Context;
using LinkBoxUI.Services.Shopify;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NPOI.SS.Formula.Functions;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace LinkBoxUI.Services.Lazada
{
    public class LazadaImageService
    {
        LinkboxDb _context = new LinkboxDb();
        LazadaTokenService tokenService = new LazadaTokenService();
        DataAccessLayer.Class.NetsuiteHelper nsHelper = new DataAccessLayer.Class.NetsuiteHelper();

        private readonly string _baseUrl = "https://api.lazada.com.ph/rest";

        // -------------------------------------------------------
        // Sync Images for a product from NetSuite to Lazada
        // -------------------------------------------------------
        //public async Task<List<string>> SyncImagesToLazada(string itemId, string sellerSku, string parentId = null)
        //{
        //    var imageUrls = new List<string>();

        //    try
        //    {
        //        var QueryEndpoint = _context.APISetups
        //            .Where(x => x.APICode.Contains("NSQueryItemData"))
        //            .Select(x => x.APIURL)
        //            .FirstOrDefault();

        //        // ← Get image IDs from NetSuite
        //        var lookupId = !string.IsNullOrEmpty(parentId) ? parentId : itemId;

        //        var check_picture = $@"
        //            SELECT
        //                custrecord_dbti_ecom_imagelink AS image_ids
        //            FROM
        //                customrecord_dbti_platform
        //            WHERE
        //                custrecord_dbti_ecom_item = {lookupId}
        //                AND custrecord_dbti_ecom_customer = 211";

        //        var internalIdsResult = await nsHelper.GetNetsuiteJson(QueryEndpoint, check_picture);

        //        var raw = internalIdsResult?
        //            .FirstOrDefault()?
        //            .image_ids?
        //            .ToString();

        //        if (string.IsNullOrWhiteSpace(raw))
        //        {
        //            Console.WriteLine($"No images found for item {itemId}");
        //            return imageUrls;
        //        }

        //        // ← Parse image IDs
        //        int[] internalIdArray = Regex.Matches(raw, @"\d+")
        //            .Cast<System.Text.RegularExpressions.Match>()
        //            .Select(m => int.Parse(m.Value))
        //            .ToArray();

        //        Console.WriteLine($"Found {internalIdArray.Length} images for item {itemId}");

        //        int position = 1;
        //        foreach (var fileId in internalIdArray)
        //        {
        //            try
        //            {
        //                // ← Check if already uploaded
        //                var existing = _context.FileCabinetImages
        //                    .FirstOrDefault(x => x.FileID == fileId.ToString()
        //                                      && x.SKUFolder == sellerSku
        //                                      && x.Platform == "Lazada");

        //                if (existing != null && !string.IsNullOrEmpty(existing.status))
        //                {
        //                    Console.WriteLine($"Image {fileId} already uploaded for {sellerSku}");
        //                    imageUrls.Add(existing.status);
        //                    position++;
        //                    continue;
        //                }

        //                // ← Get image details from NetSuite File Cabinet
        //                var imageDetails = await GetImageDetails(fileId.ToString());

        //                if (imageDetails == null || !imageDetails.success)
        //                {
        //                    Console.WriteLine($"Failed to get image details for fileId {fileId}");
        //                    position++;
        //                    continue;
        //                }

        //                var base64String = imageDetails.base64 ?? "";
        //                var imageName = imageDetails.name ?? $"{sellerSku}_{position}.jpg";

        //                if (string.IsNullOrEmpty(base64String))
        //                {
        //                    Console.WriteLine($"Empty base64 for fileId {fileId}");
        //                    position++;
        //                    continue;
        //                }

        //                // ← Convert base64 to bytes
        //                var imageBytes = Convert.FromBase64String(base64String);

        //                // ← Save image to local folder first
        //                //var savedPath = SaveImageToFolder(imageBytes, sellerSku, position, imageName);
        //                //Console.WriteLine($"Image saved to: {savedPath}");

        //                // ← Upload to Lazada
        //                var uploadResult = await UploadImageToLazada(imageBytes, imageName);
        //                Console.WriteLine($"Upload image result: {uploadResult.Body}");

        //                if (uploadResult.IsSuccess)
        //                {
        //                    // ← Parse image URL from response
        //                    var responseObj = JsonConvert.DeserializeObject<LazadaImageUploadResponse>(uploadResult.Body);
        //                    var imageUrl = responseObj?.data?.image?.url ?? "";

        //                    imageUrls.Add(imageUrl);

        //                    // ← Save to FileCabinetImages
        //                    if (existing == null)
        //                    {
        //                        _context.FileCabinetImages.Add(new DomainLayer.Models.FileCabinetImage
        //                        {
        //                            SKUFolder = sellerSku,
        //                            FileID = fileId.ToString(),
        //                            Platform = "Lazada",
        //                            ImageName = imageName,
        //                            ImageBinary = imageBytes,
        //                            CreatedDate = DateTime.Now,
        //                            status = uploadResult.Body,
        //                            ImageID = imageUrl

        //                        });
        //                    }
        //                    else
        //                    {
        //                        existing.status = uploadResult.Body;
        //                        existing.ImageID = imageUrl;
        //                        existing.UpdatedDate = DateTime.Now;
        //                        existing.ImageBinary = imageBytes;
        //                    }

        //                    _context.SaveChanges();
        //                }
        //                else
        //                {
        //                    _context.SystemLogs.Add(new DomainLayer.Models.SystemLog
        //                    {
        //                        Task = "LazadaImageService - UploadImage",
        //                        ApiUrl = $"{_baseUrl}/image/upload",
        //                        Json = imageName,
        //                        ErrorMsg = $"Error {uploadResult.StatusCode}: {uploadResult.ReasonPhrase} - {uploadResult.Body}",
        //                        CreateDate = DateTime.Now,
        //                    });
        //                    _context.SaveChanges();
        //                }

        //                position++;
        //            }
        //            catch (Exception ex)
        //            {
        //                Console.WriteLine($"Error processing image {fileId}: {ex.Message}");
        //                _context.SystemLogs.Add(new DomainLayer.Models.SystemLog
        //                {
        //                    Task = "LazadaImageService - Image Loop",
        //                    ErrorMsg = ex.Message,
        //                    CreateDate = DateTime.Now,
        //                });
        //                _context.SaveChanges();
        //                position++;
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine($"Error syncing images: {ex.Message}");
        //        _context.SystemLogs.Add(new DomainLayer.Models.SystemLog
        //        {
        //            Task = "LazadaImageService - SyncImages",
        //            ErrorMsg = ex.Message,
        //            CreateDate = DateTime.Now,
        //        });
        //        _context.SaveChanges();
        //    }

        //    return imageUrls;
        //}
        public async Task SyncImagesToLazada(string itemId, string sellerSku, string type = "Variant")
        {
            try
            {
                var QueryEndpoint = _context.APISetups
                    .Where(x => x.APICode.Contains("NSQueryItemData"))
                    .Select(x => x.APIURL)
                    .FirstOrDefault();

                // ← Get image IDs from NetSuite (source of truth)
                var check_picture = $@"
            SELECT
                custrecord_dbti_ecom_imagelink AS image_ids
            FROM
                customrecord_dbti_platform
            WHERE
                custrecord_dbti_ecom_item = {itemId}
                AND custrecord_dbti_ecom_customer = 211";

                var internalIdsResult = await nsHelper.GetNetsuiteJson(QueryEndpoint, check_picture);

                var raw = internalIdsResult?
                    .FirstOrDefault()?
                    .image_ids?
                    .ToString();

                // ← NetSuite image IDs (source of truth)
                var netsuiteFileIds = string.IsNullOrWhiteSpace(raw)
                    ? new List<string>()
                    : Regex.Matches(raw, @"\d+")
                        .Cast<System.Text.RegularExpressions.Match>()
                        .Select(m => m.Value)
                        .ToList();

                Console.WriteLine($"NetSuite has {netsuiteFileIds.Count} images for {type} - {sellerSku}");

                // ← Get existing DB records for this SKU + type
                var dbImages = _context.FileCabinetImages
                    .Where(x => x.SKUFolder == sellerSku
                             && x.Platform == "Lazada"
                             && x.Type == type)
                    .ToList();

                var dbFileIds = dbImages.Select(x => x.FileID).ToList();

                // ← New on NetSuite but not in DB → upload
                var toUpload = netsuiteFileIds.Except(dbFileIds).ToList();

                // ← In DB but removed from NetSuite → handle based on type
                var toRemove = dbFileIds.Except(netsuiteFileIds).ToList();

                //Console.WriteLine($"To upload: {toUpload.Count} | To remove: {toRemove.Count}");
                foreach (var fileId in toRemove)
                {
                    var dbRecord = dbImages.FirstOrDefault(x => x.FileID == fileId);
                    if (dbRecord == null) continue;

                    _context.FileCabinetImages.Remove(dbRecord);
                    //Console.WriteLine($"Deleted image {fileId} ({type}) from DB for {sellerSku}");
                }

                _context.SaveChanges();

                // ← Handle uploads (new NetSuite images not yet in DB)
                int position = (dbImages.Count - toRemove.Count) + 1;
                foreach (var fileId in toUpload)
                {
                    try
                    {
                        var imageDetails = await GetImageDetails(fileId);

                        if (imageDetails == null || !imageDetails.success)
                        {
                            Console.WriteLine($"Failed to get image details for fileId {fileId}");
                            continue;
                        }

                        var base64String = imageDetails.base64 ?? "";
                        var imageName = imageDetails.name ?? $"{sellerSku}_{position}.jpg";

                        if (string.IsNullOrEmpty(base64String))
                        {
                            Console.WriteLine($"Empty base64 for fileId {fileId}");
                            continue;
                        }

                        var imageBytes = Convert.FromBase64String(base64String);

                        var uploadResult = await UploadImageToLazada(imageBytes, imageName);
                        Console.WriteLine($"Upload result ({type}): {uploadResult.Body}");

                        if (uploadResult.IsSuccess && uploadResult.ErrorCode == "0")
                        {
                            var responseObj = JsonConvert.DeserializeObject<LazadaImageUploadResponse>(uploadResult.Body);
                            var imageUrl = responseObj?.data?.image?.url ?? "";

                            _context.FileCabinetImages.Add(new DomainLayer.Models.FileCabinetImage
                            {
                                SKUFolder = sellerSku,
                                FileID = fileId,
                                Platform = "Lazada",
                                ImageName = imageName,
                                ImageBinary = imageBytes,
                                CreatedDate = DateTime.Now,
                                status = uploadResult.Body,
                                ImageID = imageUrl,
                                Type = type
                            });

                            _context.SaveChanges();
                            Console.WriteLine($"Uploaded new {type} image {fileId} for {sellerSku}");
                        }
                        else
                        {
                            string ErrMes = $"Error-{uploadResult.ErrorCode}:{uploadResult.ErrorMessage}";
                            bool logAlreadyExists = _context.SystemLogs.Any(x =>
                            x.Table == sellerSku &&
                            x.ErrorMsg == ErrMes && x.Module=="Lazada");

                            if (!logAlreadyExists)
                            {
                                _context.SystemLogs.Add(new DomainLayer.Models.SystemLog
                                {
                                    Task = "LazadaImageService - UploadImage",
                                    ApiUrl = $"{_baseUrl}/image/upload",
                                    Json = uploadResult.Body,
                                    Table = sellerSku,
                                    ErrorMsg = ErrMes,
                                    Module = "Lazada",
                                    CreateDate = DateTime.Now,
                                });
                                _context.SaveChanges();
                            }
                         
                            continue;
                        }

                        position++;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error uploading image {fileId}: {ex.Message}");
                        _context.SystemLogs.Add(new DomainLayer.Models.SystemLog
                        {
                            Task = "LazadaImageService - Image Loop",
                            ErrorMsg = ex.Message,
                            CreateDate = DateTime.Now,
                        });
                        _context.SaveChanges();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error syncing images: {ex.Message}");
                _context.SystemLogs.Add(new DomainLayer.Models.SystemLog
                {
                    Task = "LazadaImageService - SyncImages",
                    ErrorMsg = ex.Message,
                    CreateDate = DateTime.Now,
                });
                _context.SaveChanges();
            }
        }

        // -------------------------------------------------------
        // Save image to local folder
        // Path: Images/Lazada/{sellerSku}/{sellerSku}_{position}.jpg
        // -------------------------------------------------------
        public string SaveImageToFolder(byte[] imageBytes, string sellerSku, int position, string imageName)
        {
            try
            {
                // ← Create folder structure
                var folderPath = Path.Combine(AppContext.BaseDirectory, "Images", "Lazada", sellerSku);
                if (!Directory.Exists(folderPath))
                    Directory.CreateDirectory(folderPath);

                // ← Save image with position-based filename
                var extension = Path.GetExtension(imageName) ?? ".jpg";
                var fileName = $"{sellerSku}_{position}{extension}";
                var filePath = Path.Combine(folderPath, fileName);

                File.WriteAllBytes(filePath, imageBytes);

                Console.WriteLine($"✅ Image saved: {filePath}");
                return filePath;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving image to folder: {ex.Message}");
                return null;
            }
        }

        // -------------------------------------------------------
        // Upload image to Lazada
        // POST /image/upload
        // -------------------------------------------------------
        public async Task<LazadaApiResult> UploadImageToLazada(byte[] imageBytes, string imageName)
        {
            try
            {
                var token = await tokenService.CallRefreshEndpoint();
                var apiPath = "/image/upload";
                var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();

                // ← Signature does NOT include image file
                var paramDict = new SortedDictionary<string, string>
                {
                    { "access_token", token.AccessToken },
                    { "app_key",      token.AppKey },
                    { "sign_method",  "sha256" },
                    { "timestamp",    timestamp }
                };

                var stringToSign = apiPath;
                foreach (var kvp in paramDict)
                    stringToSign += kvp.Key + kvp.Value;

                var sign = GenerateSignature(stringToSign, token.AppSecretKey);

                var queryString = string.Join("&",
                    paramDict.Select(kvp =>
                        $"{kvp.Key}={Uri.EscapeDataString(kvp.Value)}"));

                var fullUrl = $"{_baseUrl}{apiPath}?{queryString}&sign={sign}";

                Console.WriteLine($"Upload image URL: {fullUrl}");

                using (var client = new HttpClient())
                using (var formData = new MultipartFormDataContent())
                {
                    // ← Add image as form-data with key "image"
                    var imageContent = new ByteArrayContent(imageBytes);
                    imageContent.Headers.ContentType =
                        new MediaTypeHeaderValue("image/jpeg");

                    formData.Add(imageContent, "image", imageName);

                    var response = await client.PostAsync(fullUrl, formData);
                    var responseBody = await response.Content.ReadAsStringAsync();

                    var parsed = JObject.Parse(responseBody);
                    var code = parsed["code"]?.ToString();

                    return new LazadaApiResult
                    {
                        StatusCode = (int)response.StatusCode,
                        IsSuccess = response.IsSuccessStatusCode,
                        ReasonPhrase = response.ReasonPhrase,
                        Body = responseBody,
                        ErrorCode = code,
                        ErrorMessage = code != "0"
                        ? parsed["detail"]?[0]?["message"]?.ToString()
                          ?? parsed["message"]?.ToString()
                        : null
                    };
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"UploadImageToLazada error: {ex.Message}");
            }
        }

        // -------------------------------------------------------
        // Get image details from NetSuite File Cabinet
        // -------------------------------------------------------
        public async Task<ImageDetailsResponse> GetImageDetails(string FileId)
        {
            string accountId = ConfigurationManager.AppSettings["accountId"];
            string consumerKey = ConfigurationManager.AppSettings["consumerKey"];
            string consumerSecret = ConfigurationManager.AppSettings["consumerSecret"];
            string tokenId = ConfigurationManager.AppSettings["tokenId"];
            string tokenSecret = ConfigurationManager.AppSettings["tokenSecret"];

            string baseUrl = $"https://{accountId}.restlets.api.netsuite.com/app/site/hosting/restlet.nl";

            var queryParams = new Dictionary<string, string>
            {
                { "script", "671" },
                { "deploy", "1"   },
                { "fileId", FileId }
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

            var paramList = allParams
                .Select(kv => $"{UrlEncode(kv.Key)}={UrlEncode(kv.Value)}")
                .OrderBy(x => x)
                .ToList();

            string normalizedParams = string.Join("&", paramList);
            string signatureBase = "GET&" + UrlEncode(baseUrl) + "&" + UrlEncode(normalizedParams);
            string signingKey = UrlEncode(consumerSecret) + "&" + UrlEncode(tokenSecret);

            string signature;
            using (var hmac = new HMACSHA256(Encoding.ASCII.GetBytes(signingKey)))
                signature = Convert.ToBase64String(hmac.ComputeHash(Encoding.ASCII.GetBytes(signatureBase)));

            var authHeader = new StringBuilder();
            authHeader.Append($"OAuth realm=\"{accountId}\",");
            foreach (var kv in oauthParams)
                authHeader.Append($"{kv.Key}=\"{UrlEncode(kv.Value)}\",");
            authHeader.Append($"oauth_signature=\"{UrlEncode(signature)}\"");

            var requestUrl = baseUrl + "?" + string.Join("&",
                queryParams.Select(kv => $"{kv.Key}={UrlEncode(kv.Value)}"));

            using (var client = new HttpClient())
            using (var request = new HttpRequestMessage(HttpMethod.Get, requestUrl))
            {
                request.Headers.Add("Authorization", authHeader.ToString());
                request.Headers.Add("Accept", "application/json");

                var response = await client.SendAsync(request);
                var body = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                    throw new Exception(body);

                var obj = JsonConvert.DeserializeObject<ImageDetailsResponse>(body);

                if (obj == null || obj.success != true)
                    return null;

                return obj;
            }
        }

        // -------------------------------------------------------
        // Generate HMAC-SHA256 signature
        // -------------------------------------------------------
        private string GenerateSignature(string stringToSign, string appSecret)
        {
            var keyBytes = Encoding.UTF8.GetBytes(appSecret);
            var msgBytes = Encoding.UTF8.GetBytes(stringToSign);

            using (var hmac = new HMACSHA256(keyBytes))
            {
                var hashBytes = hmac.ComputeHash(msgBytes);
                return BitConverter.ToString(hashBytes).Replace("-", "").ToUpper();
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

    // -------------------------------------------------------
    // Models
    // -------------------------------------------------------
    //public class LazadaImageUploadResponse
    //{
    //    public string code { get; set; }
    //    public LazadaImageData data { get; set; }
    //}

    //public class LazadaImageData
    //{
    //    public LazadaImageInfo image { get; set; }
    //}

    //public class LazadaImageInfo
    //{
    //    public string url { get; set; }
    //    public string hash_code { get; set; }
    //    public string scheme { get; set; }
    //    public string type { get; set; }
    //}
}