
using ClosedXML.Excel;
using DataAccessLayer.Class;
using DocumentFormat.OpenXml.Spreadsheet;
using DomainLayer.Models;
using DomainLayer.Models.Tiktok;
using LinkBoxUI.Context;
using LinkBoxUI.Services.Shopify;
using Microsoft.Graph.Models;
using Microsoft.Graph.Reports.GetGroupArchivedPrintJobsWithGroupIdWithStartDateTimeWithEndDateTime;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NPOI.POIFS.Properties;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Numeric;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace LinkBoxUI.Services.Tiktok
{
    public class TiktokItemProductService
    {
        LinkboxDb _context = new LinkboxDb();
        NetsuiteHelper nsHelper = new NetsuiteHelper();
        NsInventoryItemToShopifyService callJson = new NsInventoryItemToShopifyService();

        //private const string AppKey = "6j705mi7urqp9";
        //private const string AppSecret = "491ea51ce5bfdd5a7d01a3a930db6dc2c1966c7e";
        //private const string ShopCipher = "ROW__BmOPQAAAACMrBYuDcN3-zzTPR8WqTGX"; //Live- "ROW_Z7lgdAAAAAAQNNI4xxel0MueZjo2Otvd";
        //private const string BaseUrl = "https://open-api.tiktokglobalshop.com";
        private static string AppKey =>
            ConfigurationManager.AppSettings["TikTokAppKey"];

        private static string AppSecret =>
       ConfigurationManager.AppSettings["TikTokAppSecret"];

        private static string ShopCipher =>
      ConfigurationManager.AppSettings["TikTokShopCipher"];

        private static string BaseUrl =>
    ConfigurationManager.AppSettings["TiktokBaseUrl"];

        private const string RequestPath = "/product/202309/products";


        public async Task PostItemtoTiktok(string code, DateTime dt)
        {
            try
            {
                var QueryEndpoint = _context.APISetups.Where(x => x.APICode.Contains("NSQueryItemData")).Select(x => x.APIURL).FirstOrDefault();

                string conz = ConfigurationManager.ConnectionStrings["SAOLinkBox"] != null ? ConfigurationManager.ConnectionStrings["SAOLinkBox"].ToString() : "";
                string query_log = $@"select top 1 last_run_time  from dbo.netsuite_item_log where log_message = 'Tiktok'";


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




                //Only Parent
                var parent_query = $@"SELECT DISTINCT
                                parent.id,
                                parent.itemid,
            	                p.custrecord_dbti_ecom_platid as product_id,
								p.custrecord_dbti_ecom_customer,
                                FROM item i
								  JOIN item parent
     								 ON parent.id = i.parent
                                JOIN customrecord_dbti_platform p
                                    ON p.custrecord_dbti_ecom_item = parent.id
                                WHERE  1=1
                                --AND i.custitem_dbti_ecom_reviseditem IS NOT NULL
                                --AND i.custitem_dbti_ecom_reviseditem = 'T's
                                AND p.custrecord_dbti_ecom_customer  = 214
                                AND i.itemid NOT LIKE 'IN-%'
                              
                                AND parent.id in (8217)
                                ";

                            //AND(
                            //   i.lastmodifieddate > TO_DATE('{lastCheckString}', 'YYYY-MM-DD HH24:MI:SS')
                            //     OR
                            //      parent.lastmodifieddate > TO_DATE('{lastCheckString}', 'YYYY-MM-DD HH24:MI:SS')
                            //)

                var parent_items = await nsHelper.GetNetsuiteJson(QueryEndpoint, parent_query);

                if (parent_items.Count == 0)
                {
                    return;
                }

                var parentIds = parent_items.Select(x => x.id.ToString()).ToList();

                var parentIdsCsv = string.Join(",", parentIds);

                if (string.IsNullOrEmpty(parentIdsCsv))
                    return;
             
                var child_query = $@"SELECT 							
								parent.id,
                                parent.itemid as custitem_skucode,
                                platform_parent.custrecord_dbti_ecom_imagelink as image_ids,
                                platform_child.custrecord_dbti_ecom_imagelink as custitem_suvariation,     
            	                platform_parent.custrecord_dbti_ecom_platid as product_id,
							    parent.custitem_dbti_title,
                                 --parent.custitem_dbti_description,
								platform_parent.custrecord_dbti_product_title as custitem_dbti_description,
								i.id as variant_id,
                                i.itemid,
                                i.displayname,
                                i.totalquantityonhand,
                                platform_child.custrecord_dbti_ecom_weight as weight,
								platform_child.custrecord_dbti_ecom_baseprice as base_price,
                                platform_child.custrecord_dbti_ecom_variation as variation,
                                platform_child.custrecord_dbti_ecom_category_list as  category
                                FROM item i
								  JOIN item parent
     								 ON parent.id = i.parent
                                JOIN customrecord_dbti_platform platform_parent
                                    ON platform_parent.custrecord_dbti_ecom_item = parent.id
								JOIN customrecord_dbti_platform platform_child
                                    ON platform_child.custrecord_dbti_ecom_item = i.id
                                WHERE  1=1
								AND platform_parent.custrecord_dbti_ecom_customer  = 214
								AND platform_child.custrecord_dbti_ecom_customer  = 214
                                --AND (i.lastmodifieddate > TO_DATE('{lastCheckString}', 'YYYY-MM-DD HH24:MI:SS'))
                                AND parent.id in ({parentIdsCsv})";
               // { parentIdsCsv}

                var parent_items_list_grouping = await nsHelper.GetNetsuiteJson(QueryEndpoint, child_query);

                if (parent_items_list_grouping.Count == 0)
                {
                    return;
                }
                //var group_by_items = parent_items_list_grouping.GroupBy(x => x.id).ToList();
                var group_by_items = parent_items_list_grouping
                .GroupBy(x => x.id)
                .Select(g => new
                {
                    ParentId = g.Key,
                    // deduplicate variants by itemid, keep first occurrence
                    Items = g.GroupBy(x => x.itemid)
                             .Select(x => x.First())
                             .ToList()
                })
                .ToList();
                foreach (var ImageManipulate    in group_by_items)
                {

                   
                    //var firstItem = ImageManipulate.FirstOrDefault();
                    var firstItem = ImageManipulate.Items.FirstOrDefault();
                    if (firstItem == null) continue; // ← guard at top

                    // --- IMAGE SYNC (Header) ---
                    var netsuiteImageIds = firstItem?.image_ids
                        ?.ToString()
                        .Split(new char[] { ',' })
                        .Where(x => !string.IsNullOrWhiteSpace(x))
                        .Select(x => x.Trim())
                        .Distinct()
                        .ToList() ?? new List<string>();

                    var skuCode = firstItem?.skucode?.ToString();
                    var dbImages = _context.FileCabinetImages
                        .Where(x => x.Platform == "Tiktok"
                                 && x.SKUFolder == skuCode)
                        .ToList();

                    var dbImageIds = dbImages.Select(x => x.FileID).ToList();

                    // ADD
                    var toAdd = netsuiteImageIds.Where(x => !dbImageIds.Contains(x)).ToList();
                    foreach (var fileId in toAdd)
                    {
                        var alreadyExists = _context.FileCabinetImages.Any(x =>
                            x.Platform == "Tiktok" &&
                            x.SKUFolder == skuCode &&
                            x.FileID == fileId &&
                            x.Type == "Header");

                        if (alreadyExists) continue; // skip silently

                        var sourceImage = await callJson.GetImageDetails(fileId.ToString());
                        var base64String = sourceImage?.base64 ?? "";
                        var imageName = sourceImage?.name ?? "";
                        byte[] imageBytes = Convert.FromBase64String(base64String.ToString());

                        var uploadResult = await UploadImageAsync(imageBytes, imageName);
                        var doc = JObject.Parse(uploadResult);
                        var message = doc["message"]?.ToString();
                        var uri = doc["data"]?["uri"]?.ToString();

                        if (message != null && message.Contains("Success"))
                        {
                            _context.FileCabinetImages.Add(new FileCabinetImage
                            {
                                SKUFolder = skuCode,
                                FileID = fileId,
                                Platform = "Tiktok",
                                ImageName = imageName,
                                ImageBinary = imageBytes,
                                Type = "Header",
                                CreatedDate = DateTime.Now,
                                status = uploadResult,
                                ImageID = uri
                            });
                        }
                    }

                    // DELETE
                    var toDelete = dbImages.Where(x => !netsuiteImageIds.Contains(x.FileID)).ToList();
                    foreach (var obsolete in toDelete)
                        _context.FileCabinetImages.Remove(obsolete);

                    await _context.SaveChangesAsync();

                    // --- IMAGE SYNC (Variant) ---
                    foreach (var skuItem in ImageManipulate.Items)
                    {
                        var skuInternalId = skuItem.custitem_suvariation?.ToString();
                        var skuFolder = skuItem.itemid?.ToString();

                        if (string.IsNullOrEmpty(skuInternalId)) continue;

                        var dbSkuImage = _context.FileCabinetImages
                            .FirstOrDefault(x => x.Platform == "Tiktok"
                                              && x.SKUFolder == skuFolder
                                              && x.Type == "Variant");

                        if (dbSkuImage != null && dbSkuImage.FileID == skuInternalId)
                            continue; // CACHE HIT — already up to date, skip

                        var skuSource = await callJson.GetImageDetails(skuInternalId);
                        var skuBase64 = skuSource?.base64 ?? "";
                        var skuImageName = skuSource?.name ?? "";
                        byte[] skuImageBytes = Convert.FromBase64String(skuBase64.ToString());

                        var skuUploadResult = await UploadImageAsync(skuImageBytes, skuImageName);
                        var skuDoc = JObject.Parse(skuUploadResult);
                        var skuMessage = skuDoc["message"]?.ToString();
                    

                        if (skuMessage != null && skuMessage.Contains("Success"))
                        {
                            var skuUri = skuDoc["data"]?["uri"]?.ToString();

                            if (dbSkuImage != null)
                            {
                                dbSkuImage.FileID = skuInternalId;
                                dbSkuImage.ImageID = skuUri;
                                dbSkuImage.ImageName = skuImageName;
                                dbSkuImage.ImageBinary = skuImageBytes;
                                dbSkuImage.UpdatedDate = DateTime.Now;
                                dbSkuImage.status = skuUploadResult;
                            }
                            else
                            {
                                _context.FileCabinetImages.Add(new FileCabinetImage
                                {
                                    SKUFolder = skuFolder,
                                    FileID = skuInternalId,
                                    Platform = "Tiktok",
                                    ImageName = skuImageName,
                                    ImageBinary = skuImageBytes,
                                    Type = "Variant",
                                    CreatedDate = DateTime.Now,
                                    status = skuUploadResult,
                                    ImageID = skuUri
                                });
                            }
                            await _context.SaveChangesAsync();
                        }
                        else
                        {
                            _context.SystemLogs.Add(new DomainLayer.Models.SystemLog
                            {
                                Task = $"Tiktok Image Uploading",
                                ApiUrl = "",
                                //Json = payload,
                                ErrorMsg = $"Error - {skuMessage}",
                                CreateDate = DateTime.Now,
                            });
                            _context.SaveChanges();
                        }
                    }

                        // --- BUILD JSON ---
                        var Product_id = firstItem.product_id?.ToString();


                    //for FDA 
                    var query = $"select name as id from customlist_dbti_ecom_category_list where id in ({firstItem.category})";
                    var cat_id = await nsHelper.GetNetSuiteData(QueryEndpoint, query);

                    string version = "v2";
                    if(cat_id.Contains("Eyeliner & Lipliner"))
                    {
                        version = "v1";
                    }
                    var Tiktok_Category_id = await GetCategoryId(cat_id,version);
                    var GetAttri = await GetAttribute(Tiktok_Category_id,version);

                    //End for FDA info

                    //var Attribute_Id = 
                    //    var mainImageUris = _context.FileCabinetImages
                    //        .Where(x => x.Platform == "Tiktok"
                    //                 && x.SKUFolder == skuCode
                    //                 && x.Type == "Header"
                    //                 && !string.IsNullOrEmpty(x.ImageID))
                    //        .Select(x => new { uri = x.ImageID })
                    //        .ToArray();

                    //    var jsonObject = new
                    //    {
                    //        title = firstItem.custitem_dbti_title,
                    //        description = firstItem.custitem_dbti_description,
                    //        category_id = Tiktok_Category_id,
                    //        category_version = "v2",
                    //        main_images = mainImageUris,
                    //        package_weight = new { value = firstItem.weight, unit = "GRAM" },
                    //        product_attributes = new[]
                    //            {
                    //                new
                    //                {
                    //                    id = "101070",
                    //                    values = new[]
                    //                    {
                    //                        new { id = "7497198912088852242" }
                    //                    }
                    //                }
                    //            },
                    //        certifications = new[]
                    //            {
                    //                new
                    //                {
                    //                    id = "8657871151245362948",
                    //                    title = "Food and Drug Administration (FDA) Certificate of Product Registration (CPR)",
                    //                    expiration_date = 1893456000,
                    //                    files = new[]
                    //                    {
                    //                        new
                    //                        {
                    //                            id = "tos-alisg-v-f466fc-sg/oADDtOtMwAAiP4ZAnhAaSLAHBaAZEyYAwAuik",
                    //                            name = "FDA SAMPLE",
                    //                            format = "JPG"
                    //                        }
                    //                    }
                    //                }
                    //            },
                    //        skus = ImageManipulate.Items.Select(skuItem =>
                    //        {
                    //            var skuBasePrice = skuItem?.sku_baseprice?.ToString() ?? "0";
                    //            var skuFolder = skuItem.itemid?.ToString();
                    //            var variantUri = _context.FileCabinetImages
                    //                .Where(x => x.Platform == "Tiktok"
                    //                         && x.SKUFolder == skuFolder
                    //                         && x.Type == "Variant"
                    //                         && !string.IsNullOrEmpty(x.ImageID))
                    //                .Select(x => x.ImageID)
                    //                .FirstOrDefault();

                    //            return new
                    //            {
                    //                seller_sku = skuItem.itemid,
                    //                price = new { amount = skuBasePrice, currency = "PHP" },
                    //                sales_attributes = new[]
                    //                {
                    //    new
                    //    {
                    //        id = "100000",
                    //        name = "Color",
                    //        value_name = skuItem.variation
                    //            ?? (skuItem.displayname?.Length > 50
                    //                ? skuItem.displayname.Substring(0, 50)
                    //                : skuItem.displayname),
                    //        //sku_img = new { uri = variantUri ?? "" }
                    //    }
                    //},
                    //                inventory = new[]
                    //                {
                    //    new
                    //    {
                    //        warehouse_id = "7133228311332996891",//"7616252375455696658",
                    //        quantity = int.TryParse(skuItem.totalquantityonhand?.ToString(), out var qty) ? qty : 0
                    //    }
                    //}
                    //            };
                    //        }).ToArray()
                    //    };


                    var mainImageUris = _context.FileCabinetImages
                        .Where(x => x.Platform == "Tiktok"
                                 && x.SKUFolder == skuCode
                                 && x.Type == "Header"
                                 && !string.IsNullOrEmpty(x.ImageID))
                        .Select(x => new { uri = x.ImageID })
                        .ToArray();

                    if (string.IsNullOrWhiteSpace(Product_id) && !mainImageUris.Any())
                    {
                        _context.SystemLogs.Add(new DomainLayer.Models.SystemLog
                        {
                            Task = "No Image Found",
                            ApiUrl = "",
                            Json = "",
                            ErrorMsg = string.IsNullOrWhiteSpace(Product_id)
                                ? "Error - Product ID is empty or not found"
                                : "Error - Main Image Uri is empty or not found",
                            CreateDate = DateTime.Now,
                        });

                        _context.SaveChanges();
                        continue;
                    }

                    var jsonObject = new Dictionary<string, object>
                    {
                      
                        ["title"] = firstItem.custitem_dbti_title,
                        ["description"] = firstItem.custitem_dbti_description ?? "SAMPLE PRODUCT DESCRIPTION",
                        ["category_id"] = Tiktok_Category_id,
                        ["category_version"] = "v2",
                        ["main_images"] = mainImageUris,
                        ["package_weight"] = new { value = firstItem.weight, unit = "GRAM" },
                        ["skus"] = ImageManipulate.Items.Select(skuItem =>
                        {
                            var skuFolder = skuItem.itemid?.ToString();
                            var skuBasePrice = skuItem?.sku_baseprice?.ToString() ?? "0";
                            var variantUri = _context.FileCabinetImages
                                    .Where(x => x.Platform == "Tiktok"
                                             && x.SKUFolder == skuFolder
                                             && x.Type == "Variant"
                                             && !string.IsNullOrEmpty(x.ImageID))
                                    .Select(x => x.ImageID)
                                    .FirstOrDefault();
                            return new
                            {
                                seller_sku = skuItem.itemid,
                                price = new { amount = skuBasePrice, currency = "PHP" },
                                sales_attributes = new[]
                                {
                                        new
                                        {
                                            id = "100000",
                                            name = "Color",
                                            value_name = skuItem.variation
                                                ?? (skuItem.displayname?.Length > 50
                                                    ? skuItem.displayname.Substring(0, 50)
                                                    : skuItem.displayname),
                                            sku_img = new { uri = variantUri ?? "" }
                                        }
                                    },
                                inventory = new[]
                                {
                                    new
                                    {
                                        warehouse_id = "7133228311332996891",//Live
                                        //warehouse_id = "7616252375455696658",//SB
                                        quantity = int.TryParse(skuItem.totalquantityonhand?.ToString(), out var qty) ? qty : 0
                                    }
                                }
                            };
                        }).ToArray()
                    };
                    if (!string.IsNullOrEmpty(firstItem.custitem_dbti_title))
                    {
                        jsonObject["title"] = firstItem.custitem_dbti_title ?? "SAMPLE TEST PRODUCT FORMULATION 25 CHARACTERS";
                    }
                    if (!string.IsNullOrEmpty(firstItem.custitem_dbti_description))
                    {
                        jsonObject["description"] = firstItem.custitem_dbti_description ?? "SAMPLE PRODUCT DESCRIPTION";
                    }

                    if (string.IsNullOrEmpty(Product_id))
                    {
                        jsonObject["save_mode"] = "AS_DRAFT";
                    }

                    if (GetAttri != null)
                    {
                        jsonObject["product_attributes"] = new[]
                        {
                            new
                            {
                                id = "101070",
                                values = new[]
                                {
                                    new { id = "7497198912088852242" }
                                }
                            }
                        };

                        jsonObject["certifications"] = new[]
                                            {
                            new
                            {
                                id = "8657871151245362948",
                                title = "Food and Drug Administration (FDA) Certificate of Product Registration (CPR)",
                                expiration_date = 1893456000,
                                files = new[]
                                {
                                    new
                                    {
                                        id = "tos-alisg-v-f466fc-sg/oADDtOtMwAAiP4ZAnhAaSLAHBaAZEyYAwAuik",
                                        name = "FDA SAMPLE",
                                        format = "JPG"
                                    }
                                }
                            }
                        };
                    }
                    // --- POST or PUT ---
                    string payload = JsonConvert.SerializeObject(jsonObject, Formatting.Indented);
                    var method = "POST";//string.IsNullOrEmpty(Product_id) ? "POST" : "PUT"; // ← fix
                    var posting_result = await PostUpdateItemProductAsync(method, payload, Product_id);

                    var jsonreturn = JObject.Parse(posting_result.Body);
                    string _code = jsonreturn["code"].ToString();
                    string _message = jsonreturn["message"].ToString();




                    // --- SAVE TO DB ---
                    if (posting_result.IsSuccess && _code=="0")
                    {
                        var obj = JObject.Parse(posting_result.Body);

                        long productId = obj["data"]?["product_id"] != null
                           ? obj["data"]["product_id"].Value<long>()
                           : 0;

                        var existing = _context.PlatformItemTable
                            .FirstOrDefault(x => x.ItemId == firstItem.id.ToString()
                                               && x.Platform == "Tiktok");
                        if (existing != null)
                        {
                            existing.ReferenceId = productId.ToString();
                            existing.PlatformJson = posting_result.Body.ToString();
                            existing.BuildJson = payload;
                            existing.UploadDate = DateTime.Now;
                            existing.Message = $"Item Updated {DateTime.Now}";
                        }
                        else
                        {
                            _context.PlatformItemTable.Add(new PlatformItemTable
                            {
                                ReferenceId = productId.ToString(),
                                Platform = "Tiktok",
                                PlatformJson = posting_result.Body.ToString(),
                                variantId = productId.ToString(),
                                CreatedDate = DateTime.Now,
                                ItemId = firstItem.id.ToString(),
                                BuildJson = payload,
                                UploadDate = DateTime.Now,
                                Message = $"Item Uploaded {DateTime.Now}"
                            });
                        }
                        _context.SaveChanges();

                        if (string.IsNullOrEmpty(Product_id))
                            await callJson.JsonCallScript(firstItem.id, 214, productId);
                    }
                    else
                    {
                        string ErrMes = $"Error-{_code}:{_message}";
                        bool logAlreadyExists = _context.SystemLogs.Any(x =>
                        x.ErrorMsg == ErrMes && x.Module == "Tiktok");
                        if (!logAlreadyExists)
                        {
                            _context.SystemLogs.Add(new DomainLayer.Models.SystemLog
                            {
                                Task = $"PostUpdateItemProductAsync",
                                ApiUrl = "",
                                Json = posting_result.Body,
                                Module = "Tiktok",
                                ErrorMsg = ErrMes,
                                CreateDate = DateTime.Now,
                            });
                            _context.SaveChanges();
                        }
                       
                        continue;
                    }
                }

                string update_query = $@"IF EXISTS (SELECT 1 FROM dbo.netsuite_item_log where log_message = 'Tiktok')
                BEGIN
                    UPDATE dbo.netsuite_item_log
                    SET last_run_time    = current_run_time,
                        current_run_time = '{dt}'
                    where log_message = 'Tiktok';
                END
                ELSE
                BEGIN
                    INSERT INTO dbo.netsuite_item_log (current_run_time, last_run_time,log_message)
                    VALUES ('{dt}', '{lastCheck}','Tiktok');
                END";
                DataAccess.Select(conz, update_query);
            }
            catch(Exception ex)
            {
                _context.SystemLogs.Add(new DomainLayer.Models.SystemLog
                {
                    Task = $"Line : Code 393",
                    ApiUrl = "PostUpdateItemProductAsync",
                    //Json = payload,
                    ErrorMsg = $"Error - {ex.Message}",
                    CreateDate = DateTime.Now,
                });
                _context.SaveChanges();
                //continue;
            }
           
        }
        

        public async Task<TiktokApiResult> PostUpdateItemProductAsync(string method, string json, string productId = null)
        {
            try
            {
                //productId = null;
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

                var timestamp = DateTimeOffset.UtcNow.AddSeconds(-60).ToUnixTimeSeconds().ToString();


                string accessToken = "";//await GetRefreshToken.RefreshAccessToken();
                var parameters = new Dictionary<string, string>
                    {
                        { "app_key", AppKey },
                        { "timestamp", timestamp },
                        { "shop_cipher", ShopCipher }
                    };

                var req_path = string.IsNullOrWhiteSpace(productId)
                    ? RequestPath
                    : $"{RequestPath}/{productId}/partial_edit";

                //string rawBody = string.IsNullOrWhiteSpace(json) ? "{}" : json.Trim();
                //string sign = GenerateSign(parameters, rawBody, req_path);
                string rawBody = string.IsNullOrWhiteSpace(json) ? "{}" : json.Trim();
                string sign = GenerateSign(parameters, rawBody, req_path);
                parameters.Add("sign", sign);          // add sign FIRST
                string query = BuildQuery(parameters); // THEN build query
                string url = BaseUrl + req_path + "?" + query;

                //string accessToken = await GetRefreshToken.RefreshAccessToken();

                using (var client = new HttpClient())
                using (var request = new HttpRequestMessage(new HttpMethod(method.ToUpperInvariant()), url))
                {
                    request.Headers.Add("x-tts-access-token", accessToken);
                    request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    //request.Content = new StringContent(json ?? "{}", Encoding.UTF8, "application/json");
                    request.Content = new StringContent(rawBody, Encoding.UTF8, "application/json");
                    HttpResponseMessage response = await client.SendAsync(request);
                    string responseBody = await response.Content.ReadAsStringAsync();

                    return new TiktokApiResult
                    {
                        StatusCode = (int)response.StatusCode,
                        IsSuccess = response.IsSuccessStatusCode,
                        ReasonPhrase = response.ReasonPhrase,
                        Body = responseBody
                    };
                }
            }
            catch(Exception ex)
            {
                throw new Exception("error - " + ex.Message);
            }
          
        }
        public async Task<string> GetCategoryId(string categoryName,string version = "v2")
        {
            try
            {
                string requestPath = "/product/202309/categories";
                
                    //
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

                string timestamp = DateTimeOffset.UtcNow.AddSeconds(-60).ToUnixTimeSeconds().ToString();

                var parameters = new Dictionary<string, string>
        {
            { "app_key", AppKey },
            { "timestamp", timestamp },
            { "shop_cipher", ShopCipher },
           // { "category_version", version }
        };

                string sign = GenerateCategorySign(parameters, requestPath);
                parameters.Add("sign", sign);

                string query = BuildQuery(parameters);
                string url = BaseUrl + requestPath + "?" + query;

                using (var client = new HttpClient())
                using (var request = new HttpRequestMessage(HttpMethod.Get, url))
                {
                    string accessToken = "";//await GetRefreshToken.RefreshAccessToken();
                    request.Headers.Add("x-tts-access-token", accessToken);

                    var response = await client.SendAsync(request);
                    response.EnsureSuccessStatusCode();

                    var json = await response.Content.ReadAsStringAsync();

                    var result = JsonConvert.DeserializeObject<TiktokCategoryResponse>(json);

                    return result?.data?.categories?
                        .FirstOrDefault(x => string.Equals(x.local_name, categoryName, StringComparison.OrdinalIgnoreCase))
                        ?.id;
                }
                    
            }
            catch (Exception ex)
            {
                throw new Exception($"Error - {ex.Message}", ex);
            }
        }
        public async Task<string> GetAttribute(string CategoryId,string version)
        {
            try
            {

                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

                string timestamp = DateTimeOffset.UtcNow.AddSeconds(-60).ToUnixTimeSeconds().ToString();

                string accessToken = "";//await GetRefreshToken.RefreshAccessToken();

                string requestPath = $"/product/202309/categories/{CategoryId}/attributes";

                var parameters = new Dictionary<string, string>
{
    { "app_key", AppKey },
    { "timestamp", timestamp },
    { "shop_cipher", ShopCipher },
    { "category_version", "v2" },
    { "access_token", accessToken },  // kept for query string, excluded from sign
};

                string sign = GenerateAttributeSign(requestPath, parameters);  // pass path here
                parameters.Add("sign", sign);

                string query = BuildQuery(parameters);
                string url = BaseUrl + requestPath + "?" + query;

                using (var client = new HttpClient())
                using (var request = new HttpRequestMessage(HttpMethod.Get, url))
                {
                    request.Headers.Add("x-tts-access-token", accessToken);

                    var response = await client.SendAsync(request);

                    if (!response.IsSuccessStatusCode)
                    {
                        var errorBody = await response.Content.ReadAsStringAsync();
                        throw new Exception($"TikTok API error {(int)response.StatusCode}: {errorBody}");
                    }
                    response.EnsureSuccessStatusCode();

                    var json = await response.Content.ReadAsStringAsync();

                    var result = JsonConvert.DeserializeObject<TiktokAttributeResponse>(json);

                    var fdaAttribute = result?.data?.attributes
                        ?.FirstOrDefault(x =>
                            x.name != null &&
                            x.name.IndexOf("FDA", StringComparison.OrdinalIgnoreCase) >= 0);

                    return fdaAttribute?.id;  // returns null if not found
                    //var fdaAttribute = result?.data?.attributes?
                    // .FirstOrDefault(x =>
                    //     x.name != null &&
                    //     x.name.Contains("FDA", StringComparison.OrdinalIgnoreCase) &&
                    //     x.is_requried);

                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error - {ex.Message}", ex);
            }
        }

        public static string GenerateCategorySign(Dictionary<string, string> parameters,string requestpath)
        {
            string requestPath = requestpath;

            string paramString = string.Concat(
                parameters
                    .OrderBy(x => x.Key, StringComparer.Ordinal)
                    .Select(x => x.Key + x.Value)
            );

            string signString = AppSecret + requestPath + paramString + AppSecret;


            using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(AppSecret)))
            {
                byte[] hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(signString));
                return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
            }
        }
        public static string GenerateAttributeSign(string requestPath, Dictionary<string, string> parameters)
        {
            var excludedKeys = new HashSet<string> { "access_token", "sign" };

            string paramString = string.Concat(
                parameters
                    .Where(x => !excludedKeys.Contains(x.Key))
                    .OrderBy(x => x.Key, StringComparer.Ordinal)
                    .Select(x => x.Key + x.Value)
            );

            string signString = AppSecret + requestPath + paramString + AppSecret;

            using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(AppSecret)))
            {
                byte[] hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(signString));
                return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
            }
        }
        public static async Task<string> UploadImageAsync(byte[] imageBytes, string fileName = "image.jpg")
        {
            var timestamp = (DateTimeOffset.UtcNow.ToUnixTimeSeconds() -60).ToString();
            var sign = GenerateSignImage(timestamp);
            var _ReqPath = "/product/202309/images/upload";
            var requestUrl = $"{BaseUrl}{_ReqPath}?app_key={AppKey}&timestamp={timestamp}&sign={sign}";
            string accessToken = "";//await GetRefreshToken.RefreshAccessToken();

            using (var httpClient = new HttpClient())
            using (var formData = new MultipartFormDataContent())
            {
                httpClient.DefaultRequestHeaders.Add("x-tts-access-token", accessToken);
                var fileContent = new ByteArrayContent(imageBytes);
                fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("image/jpeg");
                formData.Add(fileContent, "data", fileName);

                var response = await httpClient.PostAsync(requestUrl, formData);
                var responseBody = await response.Content.ReadAsStringAsync();
                return responseBody;
            }
        }

        private string GenerateSign(Dictionary<string, string> parameters, string rawBody,string reqPath)
        {
            var sorted = parameters
                .Where(p => p.Key != "sign" && p.Key != "access_token")
                .OrderBy(p => p.Key, StringComparer.Ordinal);

            var paramBuilder = new StringBuilder();

            foreach (var p in sorted)
            {
                paramBuilder.Append(p.Key).Append(p.Value);
            }

            string signString = reqPath + paramBuilder.ToString();

            if (!string.IsNullOrWhiteSpace(rawBody))
            {
                signString += rawBody.Trim();
            }

            signString = AppSecret + signString + AppSecret;

            using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(AppSecret)))
            {
                byte[] hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(signString));
                return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
            }
        }
        private static string GenerateSignImage(string timestamp, string rawBody = "")
        {
            // Build query params (sorted alphabetically, excluding sign & access_token)
            var excludeKeys = new HashSet<string> { "sign", "access_token" };

            var queryParams = new Dictionary<string, string>
        {
            { "app_key", AppKey },
            { "timestamp", timestamp }
            // Add shop_cipher here if needed:
            // { "shop_cipher", ShopCipher }
        };

            var sortedKeys = queryParams.Keys
                .Where(k => !excludeKeys.Contains(k))
                .OrderBy(k => k)
                .ToList();

            // Concatenate: key + value (no separator, no = or &)
            var paramString = string.Concat(sortedKeys.Select(k => k + queryParams[k]));

            // Build sign string: path + paramString + body (if any)
            var _ReqPath = "/product/202309/images/upload";

            var signString = _ReqPath + paramString;

            if (!string.IsNullOrEmpty(rawBody))
                signString += rawBody.Trim();

            // Wrap with app secret
            signString = AppSecret + signString + AppSecret;

            // HMAC-SHA256
         

            using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(AppSecret)))
            {
                byte[] hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(signString));
                return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
            }
        }

        //private string BuildQuery(Dictionary<string, string> parameters)
        //{
        //    return string.Join("&", parameters.Select(p =>
        //        $"{p.Key}={WebUtility.UrlEncode(p.Value)}"));
        //}
        private string BuildQuery(Dictionary<string, string> parameters)
        {
            return string.Join("&", parameters
                .OrderBy(p => p.Key, StringComparer.Ordinal)
                .Select(p => $"{p.Key}={Uri.EscapeDataString(p.Value)}"));
        }

    }
    public class TiktokCategoryResponse
    {
        public TiktokCategoryData data { get; set; }
        public int code { get; set; }
        public string message { get; set; }
        public string request_id { get; set; }
    }

    public class TiktokCategoryData
    {
        public List<TiktokCategory> categories { get; set; }
    }

    public class TiktokCategory
    {
        public string id { get; set; }
        public string local_name { get; set; }
  
    }

    public class TiktokAttributeResponse
    {
        public TiktokAttributeData data { get; set; }
        public int code { get; set; }
        public string message { get; set; }
        public string request_id { get; set; }
    }

    public class TiktokAttributeData
    {
        public List<TiktokAttribute> attributes { get; set; }
    }

    public class TiktokAttribute
    {
        public string id { get; set; }
        public string name { get; set; }

        [JsonProperty("is_requried")]  // match TikTok's typo exactly
        public bool is_required { get; set; }

        public bool is_customizable { get; set; }
        public bool is_multiple_selection { get; set; }
        public string type { get; set; }
        public List<TiktokAttributeValue> values { get; set; }
    }

    public class TiktokAttributeValue
    {
        public string id { get; set; }
        public string name { get; set; }
    }
}