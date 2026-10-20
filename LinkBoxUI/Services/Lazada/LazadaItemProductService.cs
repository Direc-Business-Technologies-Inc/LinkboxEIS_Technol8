using DataAccessLayer.Class;
using DocumentFormat.OpenXml.Wordprocessing;
using LinkBoxUI.Context;
using Microsoft.Graph;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NPOI.POIFS.Properties;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;
using static Microsoft.Win32.TaskScheduler.Events.EventQuery.CQuery;

namespace LinkBoxUI.Services.Lazada
{
    public class LazadaItemProductService
    {
        NetsuiteHelper nsHelper = new NetsuiteHelper();
        LinkboxDb _context = new LinkboxDb();
        LazadaTokenService tokenService = new LazadaTokenService();
        LazadaImageService lazadaImageService = new LazadaImageService();
        private readonly string _baseUrl = "https://api.lazada.com.ph/rest";





        public async Task SyncItems(string code, DateTime dt)
        {
            try
            {

                string conz = ConfigurationManager.ConnectionStrings["SAOLinkBox"] != null ? ConfigurationManager.ConnectionStrings["SAOLinkBox"].ToString() : "";
                string query_log = $@"select top 1 last_run_time  from dbo.netsuite_item_log where log_message = 'Lazada'";

                var dtResult = DataAccess.Select(conz, query_log);

                DateTime lastCheck;
                if (dtResult.Rows.Count == 0)
                {
                    dt = dt.AddMinutes(-2);

                    lastCheck = DateTime.Now.AddMinutes(-2);

                }
                else
                {
                    lastCheck = Convert.ToDateTime(dtResult.Rows[0]["last_run_time"]);
                }

                string lastCheckString = lastCheck.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);

                var QueryEndpoint = _context.APISetups
                    .Where(x => x.APICode.Contains("NSQueryItemData"))
                    .Select(x => x.APIURL)
                    .FirstOrDefault();

                var query = $@"SELECT
                    i.id,
                    i.itemid,
                    i.displayname,
                    i.parent,
                    i.totalquantityonhand,
                    pc.custrecord_dbti_ecom_baseprice AS base_price,
                    pp.custrecord_dbti_product_title as custitem_dbti_description,
                    pr.custitem_dbti_title,
                    i.custitem_suvariation,
                    pc.custrecord_dbti_ecom_skuid as variant_id,
                    pc.custrecord_dbti_ecom_weight AS pkg_weight,
                    pc.custrecord_dbti_weight_uom AS weight_uom,
                    pc.custrecord_dbti_ecom_length AS pkg_length,
                    pc.custrecord_dbti_ecom_variation AS color_family,
                    pc.custrecord_dbti_ecom_imagelink AS image_ids,
					pc.custrecord_dbti_ecom_category_list as category
                FROM
                    item i
                    JOIN item pr ON pr.id = i.parent
                    JOIN customrecord_dbti_platform pc ON pc.custrecord_dbti_ecom_item = i.id
                    JOIN customrecord_dbti_platform pp ON pp.custrecord_dbti_ecom_item = pr.id
					
                WHERE
					1=1
                    AND pc.custrecord_dbti_ecom_customer = 211
                    AND pp.custrecord_dbti_ecom_customer = 211
                    AND i.itemid NOT LIKE 'IN-%'
                    AND i.parent IS NOT NULL
                    --AND (i.lastmodifieddate > TO_DATE('{lastCheckString}', 'YYYY-MM-DD HH24:MI:SS'))
                    AND i.id in (8218,8219)";

                var nsItems = await nsHelper.GetNetsuiteJson(QueryEndpoint, query);

           
                if (nsItems == null || nsItems.Count == 0)
                {
                    return;
                }

                var groupedByParent = nsItems
                    .GroupBy(x => x.parent)
                    .ToList();

                foreach (var parentGroup in groupedByParent)
                {
                    try
                    {
                        var firstChild = parentGroup.First();
                        var parentId = firstChild.parent ?? "";

                        // ← Parse from ANY child that has variant_id (not just firstChild)
                        string _lazadaItemId = null;

                        foreach (var child in parentGroup)
                        {
                            var raw = child.variant_id?.ToString();
                            if (string.IsNullOrEmpty(raw)) continue;

                            var parts = raw.Split(new string[] { "_PH-" }, StringSplitOptions.None);
                            if (parts.Length == 2)
                            {
                                _lazadaItemId = parts[0]; // ← same for all variants
                                break;
                            }
                        }

                        var re_isUpdate = !string.IsNullOrEmpty(_lazadaItemId);

                        var existingParent = _context.PlatformItemTable
                            .FirstOrDefault(x => x.ItemId == parentId
                                              && x.Platform == "Lazada"
                                              && x.variantId == null);

                        if (existingParent == null && re_isUpdate)
                        {
                            existingParent = new DomainLayer.Models.PlatformItemTable
                            {
                                ReferenceId = _lazadaItemId,
                                Platform = "Lazada",
                                ItemId = parentId,
                                variantId = null,
                                CreatedDate = DateTime.Now,
                                UploadDate = DateTime.Now,
                                Message = $"Seeded from NetSuite {DateTime.Now}"
                            };
                            _context.PlatformItemTable.Add(existingParent);

                            foreach (var child in parentGroup)
                            {
                                var rawChildVariantId = child.variant_id?.ToString();
                                if (string.IsNullOrEmpty(rawChildVariantId)) continue;

                                string childLazadaItemId = null;
                                string childVariantId = null;

                                var childParts = rawChildVariantId.Split(new string[] { "_PH-" }, StringSplitOptions.None);
                                if (childParts.Length == 2)
                                {
                                    childLazadaItemId = childParts[0];
                                    childVariantId = childParts[1];
                                }

                                if (string.IsNullOrEmpty(childVariantId)) continue;

                                var existingChild = _context.PlatformItemTable
                                    .FirstOrDefault(x => x.ItemId == child.id.ToString()
                                                      && x.Platform == "Lazada"
                                                      && x.variantId != null);

                                if (existingChild == null)
                                {
                                    _context.PlatformItemTable.Add(new DomainLayer.Models.PlatformItemTable
                                    {
                                        ReferenceId = childLazadaItemId,
                                        Platform = "Lazada",
                                        ItemId = child.id.ToString(),
                                        variantId = childVariantId,
                                        CreatedDate = DateTime.Now,
                                        UploadDate = DateTime.Now,
                                        Message = $"Seeded from NetSuite {DateTime.Now}"
                                    });
                                }
                            }

                            _context.SaveChanges();


                        }

                        // ← Re-fetch after seeding so UPDATE block has correct existingParent
                        existingParent = _context.PlatformItemTable
                            .FirstOrDefault(x => x.ItemId == parentId
                                              && x.Platform == "Lazada"
                                              && x.variantId == null);

                        var isUpdate = existingParent != null;

                        var imageService = new LazadaImageService();

                        // ── Phase 1: Sync all images to DB (NetSuite is source of truth) ──
                         await imageService.SyncImagesToLazada(parentId, firstChild.itemid, "Header");

                        foreach (var child in parentGroup)
                            await imageService.SyncImagesToLazada(child.id.ToString(), child.itemid, "Variant");

                        // ── Phase 2: Read images from DB ──
                        var parentImageUrls = _context.FileCabinetImages
                            .Where(x => x.SKUFolder == firstChild.itemid
                                     && x.Platform == "Lazada"
                                     && x.Type == "Header"
                                     && !string.IsNullOrEmpty(x.ImageID))
                            .Select(x => x.ImageID)
                            .ToList();

                        var parentImagesXml = new StringBuilder();
                        foreach (var url in parentImageUrls)
                            parentImagesXml.Append($"<Image>{url}</Image>");

                        // ── Phase 3: Build SKU XML ──
                        var variantItemIds = parentGroup.Select(c => c.id.ToString()).ToList();
                        var existingVariants = _context.PlatformItemTable
                            .Where(x => x.Platform == "Lazada"
                                     && x.variantId != null
                                     && variantItemIds.Contains(x.ItemId))
                            .ToList();

                        var skusXml = new StringBuilder();

                        // ← Collect associatedSku values from existing variants only
                        //var associatedSkus = new List<string>();

                        foreach (var child in parentGroup)
                        {
                            int qty = 0;
                            int.TryParse(child.totalquantityonhand, out qty);

                            double price = 0;
                            double.TryParse(child.sku_baseprice, out price);

                            string pkg_length = "10", pkg_width = "10", pkg_height = "10";
                            if (!string.IsNullOrEmpty(child.pkg_length))
                            {
                                var parts = child.pkg_length.Split('x');
                                if (parts.Length >= 1) pkg_length = parts[0].Trim();
                                if (parts.Length >= 2) pkg_width = parts[1].Trim();
                                if (parts.Length >= 3) pkg_height = parts[2].Trim();
                            }

                            var weight = !string.IsNullOrEmpty(child.pkg_weight) ? child.pkg_weight : "0.1";

                            var existingVariant = existingVariants
                                .FirstOrDefault(x => x.ItemId == child.id.ToString());

                            // ← Collect associatedSku for existing variants
                            //if (existingVariant != null)
                            //    associatedSkus.Add(child.itemid);

                            var childImageUrls = _context.FileCabinetImages
                                .Where(x => x.SKUFolder == child.itemid
                                         && x.Platform == "Lazada"
                                         && x.Type == "Variant"
                                         && !string.IsNullOrEmpty(x.ImageID))
                                .Select(x => x.ImageID)
                                .ToList();

                            var skuImagesXml = new StringBuilder();
                            foreach (var url in childImageUrls)
                                skuImagesXml.Append($"<Image>{url}</Image>");

                            // ← No SkuId on any SKU regardless
                            skusXml.Append($@"
                                <Sku>
                                    <SellerSku>{child.itemid}</SellerSku>
                                    <color_family>{child.color_family ?? child.custitem_suvariation}</color_family>
                                    <price>{price}</price>
                                    <quantity>{qty}</quantity>
                                    <package_height>{pkg_height}</package_height>
                                    <package_width>{pkg_width}</package_width>
                                    <package_length>{pkg_length}</package_length>
                                    <package_weight>{weight}</package_weight>
                                    <Images>{skuImagesXml}</Images>
                                </Sku>");
                        }

                        // ← Build associatedSku XML for product level (existing variants only)
                        //var associatedSkuXml = new StringBuilder();
                        //foreach (var sku in associatedSkus)
                        //    associatedSkuXml.Append($"<associatedSku>{sku}</associatedSku>");

                        var firstExistingVariant = existingParent != null
                        ? _context.PlatformItemTable
                            .FirstOrDefault(x => x.ReferenceId == existingParent.ReferenceId
                                              && x.Platform == "Lazada"
                                              && x.variantId != null)
                        : null;

                        var associatedSkuXml = firstExistingVariant != null
                            ? $"<AssociatedSku>{firstExistingVariant.variantId}</AssociatedSku>"
                            : "";


                        string primaryCategoryId = "";
                        LazadaCategoryTreeResponse lazadaCategoryTree = null;
                        var categoryTreeResult = await GetCategoryTree();
                        if (categoryTreeResult.IsSuccess)
                            lazadaCategoryTree = JsonConvert.DeserializeObject<LazadaCategoryTreeResponse>(categoryTreeResult.Body);

                        if (!string.IsNullOrEmpty(firstChild.category) && lazadaCategoryTree?.data != null)
                        {
                            var queryt = $"select name as id from customlist_dbti_ecom_category_list where id in ({firstChild.category})";
                            var categoryName = await nsHelper.GetNetSuiteData(QueryEndpoint, queryt);

                            if (!string.IsNullOrEmpty(categoryName))
                            {
                                var matched = FindCategoryId(lazadaCategoryTree.data, categoryName.Trim().ToLower());
                                if (matched != null)
                                    primaryCategoryId = matched.ToString();
                                else
                                    Console.WriteLine($"Category not found in Lazada tree: {categoryName}");
                            }
                        }


                        string attributesXml = string.Empty;

                         primaryCategoryId = string.IsNullOrWhiteSpace(primaryCategoryId)
                     ? "18540"
                     : primaryCategoryId;
                        if(!isUpdate)
                        {
                            var mandatoryList = await GetMandatoryCategoryAttributes(primaryCategoryId);

                            if (mandatoryList != null && mandatoryList.Any())
                            {
                                var attribute_dict = new Dictionary<string, string>
                                    {
                                        { "brand", "Strokes Beauty Lab" },//Strokes Beauty Lab
                                        { "finish", "Natural" },
                                        { "coverage", "Full" },
                                        { "skin_type", "Normal" },
                                        { "material", "Plastic" },
                                        { "Lash_Type", "Individual" },
                                        { "formulation", "Cream" },
                                        { "product_type", "Fresh" },
                                        { "waterproof_function", "Waterproof" },
                                        { "makeup_face_benefit", "Natural" },
                                        { "sun_protection", "41 - 50 SPF" },
                                    };

                                attributesXml = string.Concat(
                                                  mandatoryList
                                                      .Where(attr => attribute_dict.ContainsKey(attr))
                                                      .Select(attr => new XElement(attr, attribute_dict[attr]).ToString())
                                              );
                            }
                        }


                        var descripty = await ProcessDescriptionImages(firstChild.custitem_dbti_description);
                        // ── Phase 4: Build payload ──

                        var nameXml = XmlElement("name", firstChild.custitem_dbti_title);
                        //var Status = XmlElement("Status", firstChild.custitem_dbti_title);      < Status > inactive </ Status >
                        var descriptionXml = XmlElement("description", descripty, wrapCData: true);

                        var payload = $@"<Request>
                        <Product>
                            <PrimaryCategory>{primaryCategoryId}</PrimaryCategory>
                      
                            <Attributes>
                                 {nameXml}
                                 {descriptionXml}
                                 {attributesXml}
                            </Attributes>
                            <Images>{parentImagesXml}</Images>
                            {associatedSkuXml}
                            <Skus>
                                {skusXml}
                            </Skus>
                        </Product>
                    </Request>";

                        //< name >{ firstChild.custitem_dbti_title}</ name >
                        //< description >< ! [CDATA[{ descripty}]]></ description >
                        Console.WriteLine($"Processing: {firstChild.displayname} with {parentGroup.Count()} variants");

                        // ── Phase 5: POST or UPDATE ──
                        if (!isUpdate)
                        {
                            var postResult = await PostProduct(payload);
                            Console.WriteLine($"POST result: {postResult.Body}");

                            if (postResult.IsSuccess && postResult.ErrorCode =="0")
                            {
                                var responseObj = JsonConvert.DeserializeObject<LazadaProductResponse>(postResult.Body);
                                var lazadaItemId = responseObj?.data?.item_id.ToString();

                                _context.PlatformItemTable.Add(new DomainLayer.Models.PlatformItemTable
                                {
                                    ReferenceId = lazadaItemId,
                                    Platform = "Lazada",
                                    ItemId = parentId,
                                    BuildJson = payload,
                                    PlatformJson = postResult.Body,
                                    CreatedDate = DateTime.Now,
                                    UploadDate = DateTime.Now,
                                    Message = $"Parent Uploaded {DateTime.Now}"
                                });

                                if (responseObj?.data?.sku_list != null)
                                {
                                    foreach (var sku in responseObj.data.sku_list)
                                    {
                                        var matchedChild = parentGroup
                                            .FirstOrDefault(x => x.itemid == sku.seller_sku);

                                        _context.PlatformItemTable.Add(new DomainLayer.Models.PlatformItemTable
                                        {
                                            ReferenceId = lazadaItemId,
                                            Platform = "Lazada",
                                            ItemId = matchedChild?.id.ToString() ?? parentId,
                                            variantId = sku.sku_id.ToString(),
                                            BuildJson = payload,
                                            PlatformJson = postResult.Body,
                                            CreatedDate = DateTime.Now,
                                            UploadDate = DateTime.Now,
                                            Message = $"Variant {sku.seller_sku} Uploaded {DateTime.Now}"
                                        });
                                    }
                                }

                                _context.SaveChanges();
                            }
                            else
                            {
                                string ErrMes = $"Error-{postResult.ErrorCode}:{postResult.ErrorMessage}";
                                bool logAlreadyExists = _context.SystemLogs.Any(x =>
                                x.ErrorMsg == ErrMes && x.Module == "Lazada");

                                if (!logAlreadyExists)
                                 {
                                    _context.SystemLogs.Add(new DomainLayer.Models.SystemLog
                                    {
                                        Task = "LazadaImageService - UploadImage",
                                        ApiUrl = $"{_baseUrl}/image/upload",
                                        Json = postResult.Body,
                                        ErrorMsg = ErrMes,
                                        Module = "Lazada",
                                        CreateDate = DateTime.Now,
                                    });
                                    _context.SaveChanges();
                                }

                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                         continue;
                            }



                            
                        }
                        else
                        {
                            // ← Always update
                            var putResult = await UpdateProduct(payload, existingParent.ReferenceId);
                            Console.WriteLine($"UPDATE result: {putResult.Body}");

                            if (putResult.IsSuccess && !putResult.Body.Contains("failed") && putResult.ErrorCode == "0")
                            {
                                var updateResponse = JsonConvert.DeserializeObject<LazadaUpdateResponse>(putResult.Body);
                                if (updateResponse?.code == "0")
                                {
                                    existingParent.BuildJson = payload;
                                    existingParent.PlatformJson = putResult.Body;
                                    existingParent.UploadDate = DateTime.Now;
                                    existingParent.Message = $"Parent Updated {DateTime.Now}";

                                    // ← Get sku_list from update response to get new variantIds
                                    var updateObj = JsonConvert.DeserializeObject<LazadaProductResponse>(putResult.Body);

                                    foreach (var child in parentGroup)
                                    {
                                        var existingChild = _context.PlatformItemTable
                                            .FirstOrDefault(x => x.ItemId == child.id.ToString()
                                                              && x.Platform == "Lazada");

                                        if (existingChild != null)
                                        {
                                            // ← Update existing variant
                                            existingChild.BuildJson = payload;
                                            existingChild.PlatformJson = putResult.Body;
                                            existingChild.UploadDate = DateTime.Now;
                                            existingChild.Message = $"Variant Updated {DateTime.Now}";
                                        }
                                        else
                                        {
                                            // ← New variant — get its sku_id from response
                                            var matchedSku = updateObj?.data?.sku_list
                                                ?.FirstOrDefault(x => x.seller_sku == child.itemid);

                                            _context.PlatformItemTable.Add(new DomainLayer.Models.PlatformItemTable
                                            {
                                                ReferenceId = existingParent.ReferenceId,
                                                Platform = "Lazada",
                                                ItemId = child.id.ToString(),
                                                variantId = matchedSku?.sku_id.ToString(),
                                                BuildJson = payload,
                                                PlatformJson = putResult.Body,
                                                CreatedDate = DateTime.Now,
                                                UploadDate = DateTime.Now,
                                                Message = $"New Variant {child.itemid} Added {DateTime.Now}"
                                            });
                                        }
                                    }

                                    _context.SaveChanges();
                                }
                            }
                            else
                            {
                                _context.SystemLogs.Add(new DomainLayer.Models.SystemLog
                                {
                                    Task = "LazadaItemProductService - UPDATE",
                                    ApiUrl = $"{_baseUrl}/product/update",
                                    Json = payload,
                                    ErrorMsg = $"Error {putResult.StatusCode}: {putResult.ReasonPhrase} - {putResult.Body}",
                                    CreateDate = DateTime.Now,
                                });
                                _context.SaveChanges();
                            }
                           
                        }
                    }
                    catch (Exception ex)
                    {
                        _context.SystemLogs.Add(new DomainLayer.Models.SystemLog
                        {
                            Task = "LazadaItemProductService - Parent Loop",
                            ErrorMsg = ex.Message,
                            CreateDate = DateTime.Now,
                        });
                        _context.SaveChanges();
                    }

                } // end foreach parentGroup

                string update_query = $@"IF EXISTS (SELECT 1 FROM dbo.netsuite_item_log where log_message = 'Lazada')
                BEGIN
                    UPDATE dbo.netsuite_item_log
                    SET last_run_time    = current_run_time,
                        current_run_time = '{dt}'
                    where log_message = 'Lazada';
                    
                END
                ELSE
                BEGIN
                    INSERT INTO dbo.netsuite_item_log (current_run_time, last_run_time,log_message)
                    VALUES ('{dt}', '{lastCheck}','Lazada');
                END";
                DataAccess.Select(conz, update_query);

            }
            catch (Exception ex)
            {
                _context.SystemLogs.Add(new DomainLayer.Models.SystemLog
                {
                    Task = "LazadaItemProductService - SyncItems",
                    ErrorMsg = ex.Message,
                    CreateDate = DateTime.Now,
                });
                _context.SaveChanges();
                throw;
            }
        }
        // -------------------------------------------------------
        // POST new product to Lazada
        // POST /product/create
        // -------------------------------------------------------

        private static string XmlElement(string tag, string value, bool wrapCData = false)
        {
            if (string.IsNullOrWhiteSpace(value)) return string.Empty;
            var content = wrapCData ? $"<![CDATA[{value}]]>" : value;
            return $"<{tag}>{content}</{tag}>";
        }
        public async Task<LazadaApiResult> PostProduct(string payload)
        {
            try
            {
                var token = await tokenService.CallRefreshEndpoint();
                var apiPath = "/product/create";
                var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();

                var signDict = new SortedDictionary<string, string>
                {
                    { "access_token", token.AccessToken },
                    { "app_key",      token.AppKey },
                    { "payload",      payload },
                    { "sign_method",  "sha256" },
                    { "timestamp",    timestamp }
                };

                var stringToSign = apiPath;
                foreach (var kvp in signDict)
                    stringToSign += kvp.Key + kvp.Value;

                var sign = GenerateSignature(stringToSign, token.AppSecretKey);

                var urlDict = new SortedDictionary<string, string>
                {
                    { "access_token", token.AccessToken },
                    { "app_key",      token.AppKey },
                    { "sign_method",  "sha256" },
                    { "timestamp",    timestamp }
                };

                var queryString = string.Join("&",
                    urlDict.Select(kvp =>
                        $"{kvp.Key}={Uri.EscapeDataString(kvp.Value)}"));

                var fullUrl = $"{_baseUrl}{apiPath}?{queryString}&sign={sign}&payload={Uri.EscapeDataString(payload)}";

                Console.WriteLine($"POST URL: {fullUrl}");

                using (var client = new HttpClient())
                {
                    var response = await client.PostAsync(fullUrl, null);
                    var responseBody = await response.Content.ReadAsStringAsync();

                    var parsed = JObject.Parse(responseBody);
                    var code = parsed["code"]?.ToString();
                    Console.WriteLine($"POST response: {responseBody}");

                    return new LazadaApiResult
                    {
                        StatusCode = (int)response.StatusCode,
                        IsSuccess = response.IsSuccessStatusCode,
                        ReasonPhrase = response.ReasonPhrase,
                        Body = responseBody,
                        ErrorCode = code,
                        ErrorMessage = code != "0"
                        ? parsed["message"]?.ToString()
                          ?? parsed["message"]?.ToString()
                        : null
                    };
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"PostProduct error: {ex.Message}");
            }
        }

        // -------------------------------------------------------
        // UPDATE existing product in Lazada
        // POST /product/update
        // -------------------------------------------------------
        public async Task<LazadaApiResult> UpdateProduct(string payload, string itemId)
        {
            try
            {
                var token = await tokenService.CallRefreshEndpoint();
                var apiPath = "/product/update";
                var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();

                var signDict = new SortedDictionary<string, string>
                {
                    { "access_token", token.AccessToken },
                    { "app_key",      token.AppKey },
                    { "payload",      payload },
                    { "sign_method",  "sha256" },
                    { "timestamp",    timestamp }
                };

                var stringToSign = apiPath;
                foreach (var kvp in signDict)
                    stringToSign += kvp.Key + kvp.Value;

                var sign = GenerateSignature(stringToSign, token.AppSecretKey);

                var urlDict = new SortedDictionary<string, string>
                {
                    { "access_token", token.AccessToken },
                    { "app_key",      token.AppKey },
                    { "sign_method",  "sha256" },
                    { "timestamp",    timestamp }
                };

                var queryString = string.Join("&",
                    urlDict.Select(kvp =>
                        $"{kvp.Key}={Uri.EscapeDataString(kvp.Value)}"));

                var fullUrl = $"{_baseUrl}{apiPath}?{queryString}&sign={sign}&payload={Uri.EscapeDataString(payload)}";

                Console.WriteLine($"UPDATE URL: {fullUrl}");

                using (var client = new HttpClient())
                {
                    var response = await client.PostAsync(fullUrl, null);
                    var responseBody = await response.Content.ReadAsStringAsync();

                    var parsed = JObject.Parse(responseBody);
                    var code = parsed["code"]?.ToString();
                    Console.WriteLine($"UPDATE response: {responseBody}");

                    return new LazadaApiResult
                    {
                        StatusCode = (int)response.StatusCode,
                        IsSuccess = response.IsSuccessStatusCode,
                        ReasonPhrase = response.ReasonPhrase,
                        Body = responseBody,
                        ErrorCode = code,
                        ErrorMessage = code != "0"
                        ? parsed["message"]?.ToString()
                          ?? parsed["message"]?.ToString()
                        : null
                    };
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"UpdateProduct error: {ex.Message}");
            }
        }

        // -------------------------------------------------------
        // UPDATE stock for a product in Lazada
        // POST /product/update with item_id
        // -------------------------------------------------------
        public async Task<LazadaApiResult> UpdateStock(string itemId, string sellerSku, string skuId, int quantity)
        {
            try
            {
                var token = await tokenService.CallRefreshEndpoint();
                var apiPath = "/product/update";
                var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();

                // ← Build stock payload
                var payload = $@"<Request>
                                    <Product>
                                        <Skus>
                                            <Sku>
                                                <SkuId>{skuId}</SkuId>
                                                <SellerSku>{sellerSku}</SellerSku>
                                                <AssociatedSku>{sellerSku}</AssociatedSku>
                                                <quantity>{quantity}</quantity>
                                            </Sku>
                                        </Skus>
                                    </Product>
                                </Request>";

                // ← Signature includes item_id
                var signDict = new SortedDictionary<string, string>
                {
                    { "access_token", token.AccessToken },
                    { "app_key",      token.AppKey },
                    { "item_id",      itemId },
                    { "payload",      payload },
                    { "sign_method",  "sha256" },
                    { "timestamp",    timestamp }
                };

                var stringToSign = apiPath;
                foreach (var kvp in signDict)
                    stringToSign += kvp.Key + kvp.Value;

                var sign = GenerateSignature(stringToSign, token.AppSecretKey);

                // ← URL includes item_id
                var urlDict = new SortedDictionary<string, string>
                {
                    { "access_token", token.AccessToken },
                    { "app_key",      token.AppKey },
                    { "item_id",      itemId },
                    { "sign_method",  "sha256" },
                    { "timestamp",    timestamp }
                };

                var queryString = string.Join("&",
                    urlDict.Select(kvp =>
                        $"{kvp.Key}={Uri.EscapeDataString(kvp.Value)}"));

                var fullUrl = $"{_baseUrl}{apiPath}?{queryString}&sign={sign}&payload={Uri.EscapeDataString(payload)}";

                Console.WriteLine($"UPDATE STOCK URL: {fullUrl}");

                using (var client = new HttpClient())
                {
                    var response = await client.PostAsync(fullUrl, null);
                    var responseBody = await response.Content.ReadAsStringAsync();

                    Console.WriteLine($"UPDATE STOCK response: {responseBody}");

                    return new LazadaApiResult
                    {
                        StatusCode = (int)response.StatusCode,
                        IsSuccess = response.IsSuccessStatusCode,
                        ReasonPhrase = response.ReasonPhrase,
                        Body = responseBody
                    };
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"UpdateStock error: {ex.Message}");
            }
        }
        private int? FindCategoryId(List<LazadaCategory> nodes, string normalizedName)
        {
            foreach (var node in nodes)
            {
                var nodeName = node.name?
                    .Trim()
                    .ToLower()
                    .Replace("&amp;", "&")
                    .Replace(" ", "");  // ← strip ALL spaces before comparing

                var searchName = normalizedName
                    .Trim()
                    .ToLower()
                    .Replace("&amp;", "&")
                    .Replace(" ", "");  // ← strip ALL spaces before comparing

                if (nodeName == searchName)
                    return node.category_id;

                if (node.children?.Any() == true)
                {
                    var found = FindCategoryId(node.children, normalizedName);
                    if (found != null) return found;
                }
            }
            return null;
        }
        private async Task<string> ProcessDescriptionImages(string html)
        {
            if (string.IsNullOrEmpty(html)) return html;

            var regex = new Regex(@"<img[^>]+src=""([^""]+)""", RegexOptions.IgnoreCase);
            var matches = regex.Matches(html);

            foreach (Match match in matches)
            {
                var originalSrc = match.Groups[1].Value;
                if (originalSrc.Contains("slatic.net")) continue;

                byte[] imageBytes = null;

                if (originalSrc.StartsWith("data:image"))
                {
                    // base64
                    var base64Data = Regex.Replace(originalSrc, @"^data:image\/[a-zA-Z]+;base64,", "");
                    imageBytes = Convert.FromBase64String(base64Data);
                }


                if (imageBytes == null || imageBytes.Length == 0) continue;

                imageBytes = ResizeIfNeeded(imageBytes, 330, 330);

                var fileName = $"desc_{Guid.NewGuid():N}.jpg";
                var lazadaResult = await lazadaImageService.UploadImageToLazada(imageBytes, fileName);

                if (string.IsNullOrEmpty(lazadaResult.Body)) continue;

                // deserialize here — not before
                var uploadResponse = JsonConvert.DeserializeObject<LazadaImageUploadResponse>(lazadaResult.Body);

                if (uploadResponse?.code != "0")
                {
                    Console.WriteLine($"Image upload failed: {lazadaResult.Body}");
                    continue;
                }

                html = html.Replace(originalSrc, uploadResponse.data.image.url);
            }

            return html;
        }
        private byte[] ResizeIfNeeded(byte[] imageBytes, int minWidth, int minHeight)
        {
            using (var ms = new MemoryStream(imageBytes))
            using (var original = Image.FromStream(ms))
            {
                if (original.Width >= minWidth && original.Height >= minHeight)
                    return imageBytes;

                var ratio = Math.Max(
                    (double)minWidth / original.Width,
                    (double)minHeight / original.Height);

                var newWidth = (int)(original.Width * ratio);
                var newHeight = (int)(original.Height * ratio);

                using (var resized = new Bitmap(newWidth, newHeight))
                using (var graphics = Graphics.FromImage(resized))
                {
                    graphics.InterpolationMode =
                        System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                    graphics.DrawImage(original, 0, 0, newWidth, newHeight);

                    using (var outMs = new MemoryStream())
                    {
                        resized.Save(outMs, ImageFormat.Jpeg);
                        return outMs.ToArray();
                    }
                }
            }
        }
        public async Task<LazadaApiResult> GetCategoryTree(string categoryname = null)
        {
            try
            {
                var token = await tokenService.CallRefreshEndpoint();
                var apiPath = "/category/tree/get";
                var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();

                var signDict = new SortedDictionary<string, string>
            {
                { "access_token", token.AccessToken },
                { "app_key",      token.AppKey },
                { "sign_method",  "sha256" },
                { "timestamp",    timestamp }
            };

                var stringToSign = apiPath;
                foreach (var kvp in signDict)
                    stringToSign += kvp.Key + kvp.Value;

                var sign = GenerateSignature(stringToSign, token.AppSecretKey);

                var urlDict = new SortedDictionary<string, string>(signDict);
                var queryString = string.Join("&",
                    urlDict.Select(kvp => $"{kvp.Key}={Uri.EscapeDataString(kvp.Value)}"));

                var fullUrl = $"{_baseUrl}{apiPath}?{queryString}&sign={sign}";

                Console.WriteLine($"GET CATEGORIES URL: {fullUrl}");

                using (var client = new HttpClient())
                {
                    var response = await client.GetAsync(fullUrl);
                    var responseBody = await response.Content.ReadAsStringAsync();

                    Console.WriteLine($"GET CATEGORIES response: {responseBody}");

                    return new LazadaApiResult
                    {
                        StatusCode = (int)response.StatusCode,
                        IsSuccess = response.IsSuccessStatusCode,
                        ReasonPhrase = response.ReasonPhrase,
                        Body = responseBody
                    };
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"GetCategoryTree error: {ex.Message}");
            }
        }
        public async Task<LazadaApiResult> GetCategoryAttribute(string categoryId)
        {
            try
            {
                var token = await tokenService.CallRefreshEndpoint();
                var apiPath = "/category/attributes/get";
                var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();

                var signDict = new SortedDictionary<string, string>
            {
                { "access_token", token.AccessToken },
                { "app_key",      token.AppKey },
                { "sign_method",  "sha256" },
                { "timestamp",    timestamp },
                { "primary_category_id",categoryId }

            };

                var stringToSign = apiPath;
                foreach (var kvp in signDict)
                    stringToSign += kvp.Key + kvp.Value;

                var sign = GenerateSignature(stringToSign, token.AppSecretKey);

                var urlDict = new SortedDictionary<string, string>(signDict);
                var queryString = string.Join("&",
                    urlDict.Select(kvp => $"{kvp.Key}={Uri.EscapeDataString(kvp.Value)}"));

                var fullUrl = $"{_baseUrl}{apiPath}?{queryString}&sign={sign}";

                Console.WriteLine($"GET CATEGORIES URL: {fullUrl}");

                using (var client = new HttpClient())
                {
                    var response = await client.GetAsync(fullUrl);
                    var responseBody = await response.Content.ReadAsStringAsync();

                    Console.WriteLine($"GET CATEGORIES response: {responseBody}");

                    return new LazadaApiResult
                    {
                        StatusCode = (int)response.StatusCode,
                        IsSuccess = response.IsSuccessStatusCode,
                        ReasonPhrase = response.ReasonPhrase,
                        Body = responseBody
                    };
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"GetCategoryTree error: {ex.Message}");
            }
        }
        public async Task<string[]> GetMandatoryCategoryAttributes(string categoryId)
        {
            var result = await GetCategoryAttribute(categoryId);

            if (!result.IsSuccess)
                throw new Exception($"Failed to get category attributes: {result.ReasonPhrase}");

            using (JsonDocument doc = JsonDocument.Parse(result.Body))
            {
                return doc.RootElement
                    .GetProperty("data")
                    .EnumerateArray()
                    .Where(attr => attr.GetProperty("is_mandatory").GetInt32() == 1)
                    .Select(attr => attr.GetProperty("name").GetString() ?? string.Empty)
                    .Where(name => !string.IsNullOrEmpty(name))
                    .ToArray();
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
    }

    // -------------------------------------------------------
    // Models
    // -------------------------------------------------------
    public class LazadaProductResponse
    {
        public string code { get; set; }
        public LazadaProductData data { get; set; }
    }

    public class LazadaProductData
    {
        public long item_id { get; set; }
        public List<LazadaSkuList> sku_list { get; set; }
    }

    public class LazadaSkuList
    {
        public string shop_sku { get; set; }
        public string seller_sku { get; set; }
        public long sku_id { get; set; }
    }

    public class LazadaUpdateResponse
    {
        public string code { get; set; }
        public LazadaUpdateData data { get; set; }
    }

    public class LazadaUpdateData
    {
        public string item_status { get; set; }
    }
    public class LazadaCategoryTreeResponse
    {
        public string code { get; set; }
        public List<LazadaCategory> data { get; set; }
    }

    public class LazadaCategory
    {
        public int category_id { get; set; }
        public string name { get; set; }
        public bool var { get; set; }       // true = this category accepts product listings
        public bool leaf { get; set; }      // true = no children, safe to use as PrimaryCategory
        public List<LazadaCategory> children { get; set; }
    }
    public class LazadaImageUploadResponse
    {
        public string code { get; set; }
        public LazadaImageUploadData data { get; set; }
    }

    public class LazadaImageUploadData
    {
        public LazadaImageUploadImage image { get; set; } // ← was missing
    }

    public class LazadaImageUploadImage
    {
        public string url { get; set; }
        public string hash_code { get; set; }
    }
}