using ClosedXML.Excel;
using DataAccessLayer.Class;
using DocumentFormat.OpenXml.Drawing.Charts;
using DomainLayer.Models;
using DomainLayer.Models.Zalora;
using LinkBoxUI.Context;
using LinkBoxUI.Helpers;
using LinkBoxUI.Services.Shopify;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection.Emit;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web.UI.WebControls;
using static DomainLayer.ViewModels.UploadViewModel;
using static ICSharpCode.SharpZipLib.Zip.FastZip;

namespace LinkBoxUI.Services.Zalora
{
    public class ZaloraItemProductServices
    {
        NetsuiteHelper nsHelper = new NetsuiteHelper();
        LinkboxDb _context = new LinkboxDb();
        ZaloraAccessTokenService accessTokenService = new ZaloraAccessTokenService();
        ZaloraImageService zaloraimage_service = new ZaloraImageService(new HttpClient());
        //ShopifyHelper shopifyHelper = new ShopifyHelper();
        public async Task SyncItems(string code, DateTime dt)
        {

            var QueryEndpoint = _context.APISetups.Where(x => x.APICode.Contains("NSQueryItemData")).Select(x => x.APIURL).FirstOrDefault();

            string conz = ConfigurationManager.ConnectionStrings["SAOLinkBox"] != null ? ConfigurationManager.ConnectionStrings["SAOLinkBox"].ToString() : "";
            string query_log = $@"select top 1 last_run_time  from dbo.netsuite_item_log where log_message = 'Zalora'";


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


            try
            {


                var query = $@"SELECT
	                                    i.id,
	                                    i.itemid,
                                        i.displayname,
                                        i.custitem_suvariation,
                                        p.custrecord_dbti_ecom_baseprice as base_price,
	                                    pr.custitem_dbti_description as description,
	                                    p.custrecord_dbti_ecom_category_list as category,
	                                    p.custrecord_dbti_ecom_catalogtype as catalogtype,
	                                    p.custrecord_dbti_ecom_subcat as subcategory,
	                                    p.custrecord_dbti_ecom_variation as variation,
	                                    p.custrecord_dbti_ecom_gender as gender,
	                                    p.custrecord_dbti_ecom_brand as brand,
                                        CASE
										 WHEN i.parent IS NOT NULL THEN pp.custrecord_dbti_product_title
										 ELSE NULL
									 END AS custrecord_dbti_product_title
                                    FROM
	                                    item i
	                                    JOIN item pr ON pr.id = i.parent
	                                    JOIN customrecord_dbti_platform p ON p.custrecord_dbti_ecom_item = i.id

                                        LEFT JOIN customrecord_dbti_platform pp
											ON pp.custrecord_dbti_ecom_item = i.parent
										   AND pp.custrecord_dbti_ecom_customer = 323
                                    WHERE
	                                    1=1
	                                    AND p.custrecord_dbti_ecom_customer = 323
	                                    AND i.itemid NOT LIKE 'IN-%'
	                                    AND i.parent is not null
                                        AND i.id in (8218)
                                        --AND (i.lastmodifieddate > TO_DATE('{lastCheckString}', 'YYYY-MM-DD HH24:MI:SS'))
                                ";


                var nsInvesntoryItems = await nsHelper.GetNetsuiteJson(QueryEndpoint, query);

                if (nsInvesntoryItems.Count == 0)
                {
                    return;
                }

                var ret_attr_resp = await GetZaloraAttributes();
                var result = Newtonsoft.Json.JsonConvert.DeserializeObject<ZaloraAttributeResponse>(
                ret_attr_resp.Body.ToString()
                );

                foreach (var items in nsInvesntoryItems)
                {
                    int quantity_onhand = int.TryParse(items.totalquantityonhand, out var qty) ? qty : 0;

                    string _longoroduct_name = "";

                    var getcat_cmd = $@"SELECT name AS id
                                        FROM customlist_dbti_ecom_category_list
                                        WHERE id = {items.category}";
                    string category = await nsHelper.GetNetSuiteData(QueryEndpoint, getcat_cmd);

                    //check if Creation or Patching
                    var check_Zalora = await CheckZaloraProduct(items.itemid);
                    long o_variant_id = 0;
                    string description = items.description;//items.description;
                    int BrandId = items.brand == "1" ? 7781 : 7945; //Strokes or Strokes Beauty
                     
                    //5680;//int.Parse(items.category); need to query this to get value

                    int gender_id = result.Items
                            .FirstOrDefault(x => x.Name == "gender")?
                            .Options.FirstOrDefault(o => o.Name == (items.gender == "1" ? "Unisex" : "Female"))?
                            .Id ?? 1;
                    int catalog_id = result.Items
                                    .FirstOrDefault(x => x.Name == "catalogtype")?
                                    .Options.FirstOrDefault(o => o.Name == (items.catalogtype == "1" ? "Main" : "Premium"))?
                                    .Id ?? 5152;

                    var subCatTypeMap = new Dictionary<string, string>
                        {
                            { "1", "4846" }, // Face Make-Up
                            { "2", "4848" }, // Eyes
                            { "3", "9582" }, // Tweezers
                            { "4", "4843" }, // Lips
                            { "5", "9556" }, // Beauty Equipment
                            { "6", "9416" }  // Cleaning Agent
                        };

                    string subCatTypeId = subCatTypeMap.ContainsKey(items.subcategory)
                        ? subCatTypeMap[items.subcategory]
                        : "9556";

                    var jsonObject = new Dictionary<string, object>
                    {
                        ["name"] = items.displayname,
                        ["brandId"] = BrandId,
                        ["primaryCategoryId"] = category,
                        ["sellerId"] = 2356,
                        ["price"] = items.sku_baseprice, // replace with your actual field
                        ["variation"] = items.variation,
                        ["attributes"] = new Dictionary<string, object>
                        {
                                        // SkuSupplierConfig
                            ["12"] = gender_id,                       // Gender
                            ["150"] = subCatTypeId,                   // SubCatType
                            ["236"] = new[] { catalog_id },         // CatalogType
                            ["59"] = items.custitem_suvariation,       // Color Main
                            ["60"] = new[] { "5249" },          // Color Family
                            ["165"] = "3008"                    // Sizing -- Constant //International
                        },
                        ["sellerSku"] = items.itemid, // ideally use items.itemid if same SKU
                        ["status"] = "active"
                    };

                    // Add only when not null/empty
                    if (!string.IsNullOrWhiteSpace(description))
                    {

                        jsonObject["description"] = description;
                    }

                    if(check_Zalora.items.Count == 0)
                    {
                        var attributes = (Dictionary<string, object>)jsonObject["attributes"];

                        if (check_Zalora.items.Count == 0)
                        {
                            attributes["11"] = items.itemid;   
                        }
                      
                    }

                    string payload = JsonConvert.SerializeObject(jsonObject, Newtonsoft.Json.Formatting.Indented);

                    var check_var_id = _context.PlatformItemTable.Where(x => x.ItemId == items.id && x.Platform == "Zalora").FirstOrDefault();

                    if (check_Zalora.items.Count > 0)
                    {

                        var product_id = check_Zalora.items.Select(x => x.id).FirstOrDefault();
                        var check_db = _context.PlatformItemTable.Where(x => x.ReferenceId == product_id.ToString() && x.Platform == "Zalora").FirstOrDefault();

                        _longoroduct_name = product_id.ToString();
                        if (check_db != null)
                        {

                            //its existing check if the payload is not the same as the payload
                            if (check_db.BuildJson != payload)
                            {
                                //update Json
                                var posting_result = await Post_Update_ItemProduct("PUT", payload, product_id.ToString());
                                var UpdatePriceLevel = await UpdatePrice(product_id.ToString(), check_var_id.variantId.ToString(), items.sku_baseprice);

                                //Update Database
                                if (posting_result.IsSuccess)
                                {
                                    check_db.UploadDate = DateTime.Now;
                                    check_db.BuildJson = payload;
                                    check_db.PlatformJson = posting_result.Body;

                                    _context.SaveChanges();
                                }
                                else
                                {
                                    _context.SystemLogs.Add(new DomainLayer.Models.SystemLog
                                    {
                                        Task = $"Line : Code 204",
                                        ApiUrl = "await Post_Update_ItemProduct(\"PUT\", payload, product_id.ToString());",
                                        Json = $"{payload}",
                                        ErrorMsg = $"Error {posting_result.StatusCode}: {posting_result.ReasonPhrase} - {posting_result.Body}",
                                        CreateDate = DateTime.Now,
                                    });
                                    _context.SaveChanges();

                                }
                            }
                            //else
                            //{
                            //    continue;
                            //}
                        }
                        else
                        {
                            var posting_result = await Post_Update_ItemProduct("PUT", payload);

                            var obj = JObject.Parse(posting_result.Body);
                            long productId = obj["id"] != null ? obj["id"].Value<long>() : 0;

                            _longoroduct_name = productId.ToString();
                            if (posting_result.IsSuccess)
                            {
                      

                                var get_product_internalid = await GetChildInternalId(productId);

                                var arr = JArray.Parse(get_product_internalid.Body);

                                // child internal id (e.g. 5637603)
                                long child_productId = arr.First?["id"]?.Value<long>() ?? 0;

                                o_variant_id = child_productId;
                                _context.PlatformItemTable.Add(new PlatformItemTable
                                {
                                    ReferenceId = product_id.ToString(),
                                    Platform = "Zalora",
                                    variantId = child_productId.ToString(),
                                    PlatformJson = posting_result.Body.ToString(),
                                    CreatedDate = DateTime.Now,
                                    ItemId = items.id.ToString(),
                                    BuildJson = payload,
                                    UploadDate = DateTime.Now,
                                    Message = $" Item Uploaded {DateTime.Now}"
                                });
                                _context.SaveChanges();
                            }
                            else
                            {
                                _context.SystemLogs.Add(new DomainLayer.Models.SystemLog
                                {
                                    Task = $"Line : Code 254",
                                    ApiUrl = "await Post_Update_ItemProduct(\"PUT\", payload, product_id.ToString());",
                                    Json = $"{payload}",
                                    ErrorMsg = $"Error {posting_result.StatusCode}: {posting_result.ReasonPhrase} - {posting_result.Body}",
                                    CreateDate = DateTime.Now,
                                });
                                _context.SaveChanges();

                            }

                            //Update Json
                            //Add to Database
                        }

                        //if existing at database 
                        //check if the json is same or not 
                        //if same continue
                        //if not then update
                        //else -- not existing


                    }
                    else
                    {
                        var posting_result = await Post_Update_ItemProduct("POST", payload);
                        var obj = JObject.Parse(posting_result.Body);
                        long productId = obj["id"] != null ? obj["id"].Value<long>() : 0;

                        _longoroduct_name = productId.ToString();

                        if (posting_result.IsSuccess)
                        {
                            var get_product_internalid = await GetChildInternalId(productId);

                            var arr = JArray.Parse(get_product_internalid.Body);

                            // child internal id (e.g. 5637603)
                            long child_productId = arr.First?["id"]?.Value<long>() ?? 0;

                            o_variant_id = child_productId;
                            _context.PlatformItemTable.Add(new PlatformItemTable
                            {
                                ReferenceId = productId.ToString(),
                                Platform = "Zalora",
                                PlatformJson = posting_result.Body.ToString(),
                                variantId = child_productId.ToString(),
                                CreatedDate = DateTime.Now,
                                ItemId = items.id.ToString(),
                                BuildJson = payload,
                                UploadDate = DateTime.Now,
                                Message = $" Item Uploaded {DateTime.Now}"
                            });
                            _context.SaveChanges();
                        }
                        else
                        {
                            _context.SystemLogs.Add(new DomainLayer.Models.SystemLog
                            {
                                Task = $"Line : Code 310",
                                ApiUrl = "await Post_Update_ItemProduct(\"POST\", payload);",
                                Json = $"{payload}",
                                ErrorMsg = $"Error {posting_result.StatusCode}: {posting_result.ReasonPhrase} - {posting_result.Body}",
                                CreateDate = DateTime.Now,
                            });
                            _context.SaveChanges();

                            continue;
                        }
                        //Add
                        //POSTING

                    }



                    //Update Stock


                    if (check_var_id != null)
                    {
                        if(quantity_onhand > 0)
                        {
                            var StockResult = await UpdateItemStock(check_var_id.variantId.ToString(), quantity_onhand);
                        }
                    }




                    //Add Image

                    var check_picture = $@"
                            SELECT
                                custrecord_dbti_ecom_imagelink AS image_ids
                            FROM
                                customrecord_dbti_platform
                            WHERE
                                custrecord_dbti_ecom_item = {items.id}
                                AND custrecord_dbti_ecom_customer = 323
                        ";

                    var internalIdsResult = await nsHelper.GetNetsuiteJson(QueryEndpoint, check_picture);

                    var raw = internalIdsResult?
                                .FirstOrDefault()?
                                .image_ids?
                                .ToString();

                    int[] internalIdArray = Array.Empty<int>();

                    if (!string.IsNullOrWhiteSpace(raw))
                    {
                        internalIdArray = Regex.Matches(raw, @"\d+")
                                               .Cast<Match>()
                                               .Select(m => int.Parse(m.Value))
                                               .ToArray();



      
                        // With this line:
                        var img_result = await zaloraimage_service.GetImagesAsync(items.itemid, internalIdArray.Select(x => x.ToString()));

                        if (img_result.Count > 0){
                            //posting
                     
                            foreach(var image_details in img_result)
                            {
                                if (image_details.Error != null)
                                {
                                    _context.SystemLogs.Add(new DomainLayer.Models.SystemLog
                                    {
                                        Task = $"img_result",

                                        ErrorMsg = $"Error  - Image ID {image_details.InternalId} - {image_details.Error}",
                                        CreateDate = DateTime.Now,
                                    });
                                    _context.SaveChanges();
                                    continue;
                                }
                                var checkCabinet = _context.FileCabinetImages.Where(x => x.SKUFolder == items.itemid && x.FileID == image_details.InternalId && x.ImageName == image_details.SavedFileName).FirstOrDefault();

                                if (checkCabinet != null)
                                {
                                    continue;
                                }
                                int position = GetImagePosition(image_details.SavedFileName);
                                var posting_image_result = await PostImagetoZalora(_longoroduct_name, image_details, position);

                                if (!string.IsNullOrEmpty(posting_image_result.duplicate_Id))
                                {
                                    //var update_db = _context.FileCabinetImages.Where(x => x.FileID == image_details.InternalId).FirstOrDefault();
                                    var update_db = _context.FileCabinetImages
                                                 .Where(x => x.FileID == image_details.InternalId)
                                                 .OrderByDescending(x => x.CreatedDate)
                                                 .FirstOrDefault();
                                    var patch_Res = await UpdateImagePosition(_longoroduct_name,position, update_db.ImageID);

                                 
                                    if (patch_Res.IsSuccess)
                                    {
                                        if (update_db != null)
                                        {
                                            update_db.ImageName = image_details.SavedFileName;
                                            update_db.UpdatedDate = DateTime.Now;
                                            _context.SaveChanges();
                                        }
                                    }
                                    else
                                    {
                                        _context.SystemLogs.Add(new DomainLayer.Models.SystemLog
                                        {
                                            Task = $"UpdateImagePosition",
                                            ApiUrl = image_details.DisplayUrl,
                                            Json = patch_Res.jsonBody,
                                            ErrorMsg = $"Error  - {DateTime.Now} - {patch_Res.Body} - {patch_Res.ReasonPhrase}",
                                            CreateDate = DateTime.Now,
                                        });
                                        _context.SaveChanges();
                                    }

                                    continue;
                                }

                                if (posting_image_result.IsSuccess)
                                {
                                    long imageId = 0;
                                    using (JsonDocument doc = JsonDocument.Parse(posting_image_result.Body))
                                    {
                                         imageId = doc.RootElement
                                                   .GetProperty("id")
                                                   .GetInt64();
                                    }
                                    _context.FileCabinetImages.Add(new FileCabinetImage
                                    {
                                        SKUFolder = items.itemid,
                                        FileID = image_details.InternalId,
                                        Platform = "Zalora",
                                        ImageID = imageId.ToString(),
                                        ImageName = image_details.SavedFileName,
                                        Type = "",
                                        CreatedDate = DateTime.Now,
                                        status = posting_image_result.Body.ToString(),
                                    });

                                    _context.SaveChanges();
                                }
                                else
                                {
                                    _context.SystemLogs.Add(new DomainLayer.Models.SystemLog
                                    {
                                        Task = $"PostImagetoZalora",
                                        ApiUrl = image_details.DisplayUrl,
                                        Json = posting_image_result.jsonBody,
                                        ErrorMsg = $"Error  - {DateTime.Now} - {posting_image_result.Body} - {posting_image_result.ReasonPhrase}",
                                        CreateDate = DateTime.Now,
                                    });
                                    _context.SaveChanges();
                                }
                            }
                        }
                    }

                }//end of foreach


                string update_query = $@"IF EXISTS (SELECT 1 FROM dbo.netsuite_item_log where log_message = 'Zalora')
                BEGIN
                    UPDATE dbo.netsuite_item_log
                    SET last_run_time    = current_run_time,
                        current_run_time = '{dt}'
                    WHERE log_message = 'Zalora';   
                END
                ELSE
                BEGIN
                    INSERT INTO dbo.netsuite_item_log (current_run_time, last_run_time,log_message)
                    VALUES ('{dt}', '{lastCheck}','Zalora');
                END";
                DataAccess.Select(conz, update_query);
            }
            catch (Exception ex)
            {
                throw;
                //Console.WriteLine($"Error syncing items for mapid {mapid}: {ex.Message}");
            }
            
            
        }
        public static int GetImagePosition(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                throw new ArgumentException("File name is required.", nameof(fileName));

            // Extract filename without path
            var name = Path.GetFileNameWithoutExtension(fileName);

            // Match _<number> at the end
            var match = Regex.Match(name, @"_(\d+)$");

            if (match.Success && int.TryParse(match.Groups[1].Value, out int position))
            {
                return position;
            }

            // Default position if no suffix found
            return 1;
        }
        public async Task<ZaloraApiResult> GetZaloraAttributes()
        {
            try
            {

                var url = "https://sellercenter-api.zalora.com.ph/v2/attributes?attributeIds[]=11&attributeIds[]=12&attributeIds[]=150&attributeIds[]=236&attributeIds[]=59&attributeIds[]=60";

                //var query = string.Join("&",
                //    attributeIds.Select(id => $"attributeIds[]={id}")
                //);

                //var url = $"{baseUrl}?{query}";

                var accessToken = await accessTokenService.AccessToken();
                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", accessToken);
                    client.DefaultRequestHeaders.Accept.Add(
                        new MediaTypeWithQualityHeaderValue("application/json")
                    );

                    var response = await client.GetAsync(url);
                    var responseBody = await response.Content.ReadAsStringAsync();

                    if (response.StatusCode == HttpStatusCode.NotFound)
                    {
                        return new ZaloraApiResult
                        {
                            StatusCode = 404,
                            IsSuccess = true,
                            Body = responseBody
                        };
                    }

                    return new ZaloraApiResult
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
                throw;
            }


        }

        public async Task<ZaloraApiResult> GetChildInternalId(long internalid)
        {
            try
            {

                var url = $"https://sellercenter-api.zalora.com.ph/v2/product-set/{internalid}/products";

                var accessToken = await accessTokenService.AccessToken();
                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", accessToken);
                    client.DefaultRequestHeaders.Accept.Add(
                        new MediaTypeWithQualityHeaderValue("application/json")
                    );

                    var response = await client.GetAsync(url);
                    var responseBody = await response.Content.ReadAsStringAsync();

                    if (response.StatusCode == HttpStatusCode.NotFound)
                    {
                        return new ZaloraApiResult
                        {
                            StatusCode = 404,
                            IsSuccess = true,
                            Body = responseBody
                        };
                    }

                    return new ZaloraApiResult
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
                throw;
            }


        }


        public async Task<ZaloraItemModel> CheckZaloraProduct(string ParentSKU)
        {
            try
            {
                // 1️⃣ Get cached bearer token (will NOT re-call if valid)
                var accessToken = await accessTokenService.AccessToken();

               


                //var urlBuilder = new StringBuilder($"https://sellercenter-api.zalora.com.ph/v2/orders?orderNumbers[]=264712847&orderNumbers[]=279712847");
                var urlBuilder = new StringBuilder($"https://sellercenter-api.zalora.com.ph/v2/product-sets?parentSku={ParentSKU}");

                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Authorization =
                        new AuthenticationHeaderValue("Bearer", accessToken);

                    var response = await client.GetAsync(urlBuilder.ToString());

                    // 5️⃣ Throw if NOT success
                    if (!response.IsSuccessStatusCode)
                    {
                        var errorBody = await response.Content.ReadAsStringAsync();
                        throw new HttpRequestException(
                            $"Zalora orders request failed. " +
                            $"Status: {(int)response.StatusCode}. Body: {errorBody}");
                    }

                    // 6️⃣ Return response (JSON)
                    var json = await response.Content.ReadAsStringAsync();

                    var result = Newtonsoft.Json.JsonConvert.DeserializeObject<ZaloraItemModel>(json);

                    // Return strongly-typed object
                    return result;
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error fetching pending orders: " + ex.Message, ex);
            }
         
        }
        public async Task<ZaloraApiResult> Post_Update_ItemProduct(string method, string json, string productid = null)
        {
            //productid = "1548672";
            if (string.IsNullOrWhiteSpace(method))
                throw new ArgumentException("Method is required.", nameof(method));

            if (string.IsNullOrWhiteSpace(json))
                throw new ArgumentException("JSON payload is required.", nameof(json));

            method = method.Trim().ToUpperInvariant();

            string url;
            if (method == "POST")
            {
                url = "https://sellercenter-api.zalora.com.ph/v2/product-set";
            }   
            else
            {
                if (string.IsNullOrWhiteSpace(productid))
                    throw new ArgumentException("productid is required for update requests.", nameof(productid));

                url = $"https://sellercenter-api.zalora.com.ph/v2/product-set/{productid}";
            }

            var accessToken = await accessTokenService.AccessToken();

            using (var client = new HttpClient())
            using (var content = new StringContent(json, Encoding.UTF8, "application/json"))
            {
                client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", accessToken);

                client.DefaultRequestHeaders.Accept.Add(
                    new MediaTypeWithQualityHeaderValue("application/json"));

                HttpResponseMessage response;

                // Use actual HTTP verb
                switch (method)
                {
                    case "POST":
                        response = await client.PostAsync(url, content);
                        break;

                    case "PUT":
                        response = await client.PutAsync(url, content);
                        break;

                    case "PATCH":
                        var patchRequest = new HttpRequestMessage(new HttpMethod("PATCH"), url)
                        {
                            Content = content
                        };
                        response = await client.SendAsync(patchRequest);
                        break;

                    default:
                        throw new NotSupportedException($"HTTP method '{method}' is not supported.");
                }

                var responseBody = await response.Content.ReadAsStringAsync();

                return new ZaloraApiResult
                {
                    StatusCode = (int)response.StatusCode,
                    IsSuccess = response.IsSuccessStatusCode, // 404 stays false
                    ReasonPhrase = response.ReasonPhrase,
                    Body = responseBody
                };
            }
        }

        public async Task<ZaloraApiResult> UpdateItemStock(string variantid, int stock)
        {
            if (string.IsNullOrWhiteSpace(variantid))
                throw new ArgumentException("variantid is required.", nameof(variantid));

            if (stock < 0)
                throw new ArgumentException("stock cannot be negative.", nameof(stock));

            // TODO: Confirm the exact Zalora stock endpoint
            var url = "https://sellercenter-api.zalora.com.ph/v2/stock/product";

            var accessToken = await accessTokenService.AccessToken();

            // Stock update payload as JSON array
            //stock = 22;
            var payloadObject = new[]
            {
                new
                {
                    productId = variantid, // or convert to long if required by API
                    quantity = stock
                }
            };

            string json = JsonConvert.SerializeObject(payloadObject);

            using (var client = new HttpClient())
            using (var content = new StringContent(json, Encoding.UTF8, "application/json"))
            {
                client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", accessToken);

                client.DefaultRequestHeaders.Accept.Add(
                    new MediaTypeWithQualityHeaderValue("application/json"));

                var response = await client.PutAsync(url, content);
                var responseBody = await response.Content.ReadAsStringAsync();

                return new ZaloraApiResult
                {
                    StatusCode = (int)response.StatusCode,
                    IsSuccess = response.IsSuccessStatusCode,
                    ReasonPhrase = response.ReasonPhrase,
                    Body = responseBody
                };
            }
        }
        public async Task<ZaloraApiResult> UpdatePrice(string ProductId,string VariantId, string price)
        {


            // TODO: Confirm the exact Zalora stock endpoint
            var url = $"https://sellercenter-api.zalora.com.ph/v2/product-set/{ProductId}/prices";

            var accessToken = await accessTokenService.AccessToken();

            // Stock update payload as JSON array
            //stock = 22;
            var payloadObject = new[]
            {
                new
                {
                    productId = VariantId, // or convert to long if required by API
                    country = "PH",
                    price = price
                }
            };

            string json = JsonConvert.SerializeObject(payloadObject);

            using (var client = new HttpClient())
            using (var content = new StringContent(json, Encoding.UTF8, "application/json"))
            {
                client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", accessToken);

                client.DefaultRequestHeaders.Accept.Add(
                    new MediaTypeWithQualityHeaderValue("application/json"));

                var response = await client.PutAsync(url, content);
                var responseBody = await response.Content.ReadAsStringAsync();

                return new ZaloraApiResult
                {
                    StatusCode = (int)response.StatusCode,
                    IsSuccess = response.IsSuccessStatusCode,
                    ReasonPhrase = response.ReasonPhrase,
                    Body = responseBody
                };
            }
        }
        public async Task<ZaloraApiResult> PostImagetoZalora(string itemid, ImageResponseModel model,int position)
        {
            ZaloraApiResult lastResult = null;

            // TODO: Confirm the exact Zalora stock endpoint
            var url = $"https://sellercenter-api.zalora.com.ph/v2/product-set/{itemid}/images";

            var accessToken = await accessTokenService.AccessToken();

            //foreach (var items in model)
            //{
            var ngrok = "https://unglossed-welcomely-edmund.ngrok-free.dev";
                //"https://unglossed-welcomely-edmund.ngrok-free.dev";
            var payloadObject = new
            {
                position = position,
                displayUrl = model.DisplayUrl
                                .Replace("https://localhost:7242", ngrok)
                                .Replace("https://localhost:8081", ngrok)
                                .Replace("https://159.138.99.111:8085", ngrok)
                                ,
                overwrite = true
            };

            string json = JsonConvert.SerializeObject(payloadObject);

                using (var client = new HttpClient())
                using (var content = new StringContent(json, Encoding.UTF8, "application/json"))
                {
                    client.DefaultRequestHeaders.Authorization =
                        new AuthenticationHeaderValue("Bearer", accessToken);

                    client.DefaultRequestHeaders.Accept.Add(
                        new MediaTypeWithQualityHeaderValue("application/json"));

                    var response = await client.PostAsync(url, content);
                    var responseBody = await response.Content.ReadAsStringAsync();

                string duplicated_id = null;
                if (!response.IsSuccessStatusCode)
                {
                    var match = Regex.Match(responseBody, @"ID:\s*(\d+)");

                    if (match.Success)
                    {
                        duplicated_id = match.Groups[1].Value.ToString();
                    }
                        
                }
                    lastResult = new ZaloraApiResult
                    {
                        StatusCode = (int)response.StatusCode,
                        IsSuccess = response.IsSuccessStatusCode,
                        ReasonPhrase = response.ReasonPhrase,
                        Body = responseBody,
                        jsonBody = json,
                        duplicate_Id = duplicated_id

                    };
                }
            //}

            return lastResult;
        }

        public async Task<ZaloraApiResult> UpdateImagePosition(
         string itemid,
         int position,
         string image_id)
        {
            var url = $"https://sellercenter-api.zalora.com.ph/v2/product-set/{itemid}/images/{image_id}";

            var accessToken = await accessTokenService.AccessToken();

            var payloadObject = new
            {
                position = position
            };

            string json = JsonConvert.SerializeObject(payloadObject);

            using (var client = new HttpClient())
            using (var content = new StringContent(json, Encoding.UTF8, "application/json")) // <-- FIXED: declare 'content'
            {
                var request = new HttpRequestMessage(new HttpMethod("PATCH"), url)
                {
                    Content = content
                };

                request.Headers.Authorization =
                    new AuthenticationHeaderValue("Bearer", accessToken);

                request.Headers.Accept.Add(
                    new MediaTypeWithQualityHeaderValue("application/json"));

                var response = await client.SendAsync(request);
                var responseBody = await response.Content.ReadAsStringAsync();

                return new ZaloraApiResult
                {
                    StatusCode = (int)response.StatusCode,
                    IsSuccess = response.IsSuccessStatusCode,
                    ReasonPhrase = response.ReasonPhrase,
                    Body = responseBody,
                    jsonBody = json
                };
            }
        }

    }













    public class ZaloraAttributeResponse
    {
        [JsonPropertyName("items")]
        public List<ZaloraAttribute> Items { get; set; } = new List<ZaloraAttribute>();
    }

    public class ZaloraAttribute
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("label")]
        public string Label { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("isMandatory")]
        public bool IsMandatory { get; set; }

        [JsonPropertyName("exampleValue")]
        public string ExampleValue { get; set; } = string.Empty;

        [JsonPropertyName("maxLength")]
        public int? MaxLength { get; set; }

        [JsonPropertyName("options")]
        public List<ZaloraAttributeOption> Options { get; set; } = new List<ZaloraAttributeOption>();
    }

    public class ZaloraAttributeOption
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("isDefault")]
        public bool IsDefault { get; set; }
    }
}