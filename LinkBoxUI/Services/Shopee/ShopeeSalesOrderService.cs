
//using ClosedXML.Excel;
//using DataAccessLayer.Class;
//using DomainLayer.Models;
//using LinkBoxUI.Context;
//using LinkBoxUI.Services.Shopify;
//using Newtonsoft.Json;
//using Newtonsoft.Json.Linq;
//using System;
//using System.Collections.Generic;
//using System.Configuration;
//using System.Globalization;
//using System.IO;
//using System.Linq;
//using System.Net.Http;
//using System.Security.Cryptography;
//using System.Text;
//using System.Threading;
//using System.Threading.Tasks;
//using System.Web.Helpers;

//namespace LinkBoxUI.Services.Shopee
//{
//    public class ShopeeItemProductService
//    {
//        LinkboxDb _context = new LinkboxDb();
//        NetsuiteHelper nsHelper = new NetsuiteHelper();
//        ShopeeTokenService tokenService = new ShopeeTokenService();
//        NsInventoryItemToShopifyService callJson = new NsInventoryItemToShopifyService();

//        private readonly string _baseUrl = "https://openplatform.sandbox.test-stable.shopee.sg";
//        private readonly string _partnerId = "1197442";
//        private readonly string _partnerKey = "shpk55774e634d44507462416d6c565047424273546f73426653745351516463";
//        private readonly string _shopId = "226172448";
//        public async Task SyncShopeItems(string Code, DateTime dt)//int[] Mapid, DateTime dt
//        {

//            var QueryEndpoint = _context.APISetups.Where(x => x.APICode.Contains("NSQueryItemData")).Select(x => x.APIURL).FirstOrDefault();

//            string conz = ConfigurationManager.ConnectionStrings["SAOLinkBox"] != null ? ConfigurationManager.ConnectionStrings["SAOLinkBox"].ToString() : "";
//            string query_log = $@"select top 1 last_run_time  from dbo.netsuite_item_log where log_message = 'Shopee'";


//            var dtResult = DataAccess.Select(conz, query_log);

//            DateTime lastCheck;
//            if (dtResult.Rows.Count == 0)
//            {
//                dt = dt.AddMinutes(-2);

//                lastCheck = DateTime.Now.AddMinutes(-2);

//            }
//            else
//            {
//                lastCheck = Convert.ToDateTime(dtResult.Rows[0]["last_run_time"]);
//            }

//            string lastCheckString = lastCheck.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);



//            try
//            {


//                var query = $@"SELECT
//                                i.itemid,
//                                i.displayname,
//                                i.totalquantityonhand,
//                                i.parent,
//                                 parent.itemid  as custitem_skucode,
//								 parent.custitem_dbti_title,
//								 parent.custitem_dbti_description,

//								platform_parent.custrecord_dbti_ecom_platid as product_id,
//								p.custrecord_dbti_ecom_platid as product_id_v2,
//            	                i.custitem_su_shpid as shopee_id,
                                
//            	                p.custrecord_dbti_ecom_variation as variation,
//				                p.custrecord_dbti_ecom_baseprice as base_price,
//				                p.custrecord_dbti_ecom_skuid as variant_id
//                                FROM item i
//                                JOIN customrecord_dbti_platform p
//                                    ON p.custrecord_dbti_ecom_item = i.id
//                                JOIN item parent
//     								 ON parent.id = i.parent
//                                JOIN customrecord_dbti_platform platform_parent
//                                    ON platform_parent.custrecord_dbti_ecom_item = parent.id
//                                WHERE  1=1
//                                AND i.custitem_dbti_ecom_reviseditem IS NOT NULL
//                                AND i.custitem_dbti_ecom_reviseditem = 'T'
//                                AND p.custrecord_dbti_ecom_customer  = 213
//	                            AND platform_parent.custrecord_dbti_ecom_customer in (213)
//                                --AND i.id in (8205)
                              
//                                ORDER BY
//                                CASE 
//                                    WHEN i.parent IS NULL THEN 0 
//                                    ELSE 1  
//                                END,
//                                i.parent
                               
//                                ";

//                var nsInventoryItems = await nsHelper.GetNetsuiteJson(QueryEndpoint, query);


//                var group_by_items = nsInventoryItems
//                .GroupBy(x => x.skucode)
//                .Select(g => new
//                {
//                    ParentId = g.Key,
//                    // deduplicate variants by itemid, keep first occurrence
//                    Items = g.GroupBy(x => x.itemid)
//                             .Select(x => x.First())
//                             .ToList()
//                })
//                .ToList();

//                foreach (var items in group_by_items)
//                {
//                    var firstItem = items.Items.FirstOrDefault();

//                    var ShopeeProduct_Id = firstItem.product_id; //!= null ? items.product_id : items.product_id_v2;

//                    if (string.IsNullOrEmpty(ShopeeProduct_Id)) // Dont have product id it means first time to post
//                    {

//                        //Post New Product to Shopee
//                        var jsonObject = new
//                        {
//                            item_name = firstItem.custitem_dbti_title,
//                            item_sku = firstItem.skucode,
//                            description = firstItem.custitem_dbti_description,
//                            original_price = double.TryParse(firstItem.sku_baseprice, out var price) ? price : 0,
//                            weight = double.TryParse(firstItem.weight, out var weight) && weight > 0 ? weight : 0.1,
//                            category_id = 301634,//await GetCategoryId(cat_id),
//                            brand = new
//                            {
//                                brand_id = 1704696,//firstItem.brand_id
//                            },
//                            image = new
//                            {
//                                image_id_list = new[] { "ph-11134207-81zth-micat5j4ulfo41" }
//                            },
//                            logistic_info = new[]
//                        {
//                            new
//                            {
//                                logistic_id = 41003,
//                                enabled = true,
//                                is_free = false,
//                                size_id = 0
//                            },
//                            new
//                            {
//                                logistic_id = 41004,
//                                enabled = true,
//                                is_free = false,
//                                size_id = 0
//                            }
//                        },
//                            seller_stock = new[]
//                        {
//                            new
//                            {
//                                location_id = "PHZ",
//                                stock = 0
//                            }
//                        }
//                        };

//                        string payload = JsonConvert.SerializeObject(jsonObject, Formatting.Indented);

//                        var posting_result = await AddItem(payload);
//                        if (posting_result.StatusCode == 200 && posting_result.ReasonPhrase == "OK" && !posting_result.Body.Contains("Failed to create product"))
//                        {

//                            var json = JObject.Parse(posting_result.Body);
//                            var itemId = json["response"]?["item_id"]?.Value<long>();


//                            await callJson.JsonCallScript(firstItem.parent, 213, long.Parse(itemId.ToString()));

//                            if (itemId.HasValue)
//                            {
//                                var init_tierPayloadObject = new
//                                {
//                                    item_id = itemId,
//                                    tier_variation = new[]
//                                  {
//                                        new
//                                        {
//                                            name = "Variation",
//                                            option_list = new[]
//                                            {
//                                                new
//                                                {
//                                                    option = string.IsNullOrEmpty(firstItem.variation)
//                                                                    ? ""
//                                                                    : (firstItem.variation.Length > 25
//                                                                        ? firstItem.variation.Substring(0, 25)
//                                                                        : firstItem.variation)
//                                            }
//                                            }
//                                        }
//                                    },
//                                    model = new[]
//                                  {
//                                        new
//                                        {
//                                            tier_index = new[] { 0 },
//                                            model_sku = firstItem.itemid,
//                                            original_price = double.TryParse(firstItem.sku_baseprice, out var _price) ? _price : 0,
//                                            seller_stock = new[]
//                                            {
//                                                new
//                                                {
//                                                    location_id = "PHZ",
//                                                    stock = int.TryParse(firstItem.totalquantityonhand?.ToString(), out var stock) ? stock : 0
//                                                }
//                                            }
//                                        }
//                                    }
//                                };

//                                string tierPayload = JsonConvert.SerializeObject(init_tierPayloadObject, Formatting.Indented);

//                                var tierResult = await InitTierVariation(tierPayload);
//                            }

//                        }

//                        //Update Header Product ID

//                    }
//                    else
//                    {
//                        //update
//                        //check if parent
//                        var modelList = await GetModelList(ShopeeProduct_Id);

//                        if (modelList.StatusCode == 200 && modelList.ReasonPhrase.Contains("OK"))
//                        {
//                            var obj = JObject.Parse(modelList.Body);
//                            var tierVariation = (JArray)obj["response"]?["tier_variation"];

//                            if (tierVariation != null && tierVariation.Count > 0)
//                            {
//                                // ✅ Build newTiers dynamically from your NetSuite items
//                                var newTiers = new List<TierVariationInput>
//                                    {
//                                        new TierVariationInput
//                                        {
//                                            Name = "Variation", // Must match your existing tier name in Shopee
//                                            Options = items.Items.Select(i => new TierOptionInput
//                                            {
//                                                Option = string.IsNullOrEmpty(i.variation)
//                                                    ? i.itemid
//                                                    : (i.variation.Length > 25
//                                                        ? i.variation.Substring(0, 25)
//                                                        : i.variation),
//                                                ImageId = null // populate if you have image per variant
//                                            }).ToList()
//                                        }
//                                    };

//                                // ✅ Sync tier variations
//                                bool tierSynced = await SyncTierVariations(ShopeeProduct_Id, newTiers, tierVariation);

//                                if (tierSynced)
//                                {
//                                    // ✅ Re-fetch model list to get updated tier indexes
//                                    var refreshedModelList = await GetModelList(ShopeeProduct_Id);
//                                    var refreshedObj = JObject.Parse(refreshedModelList.Body);
//                                    var refreshedTiers = (JArray)refreshedObj["response"]?["tier_variation"];

//                                    // ✅ Build model inputs — map each NS item to its tier_index position
//                                    var newModels = BuildModelInputs(items.Items, refreshedTiers);
//                                    await SyncModels(ShopeeProduct_Id, newModels, refreshedObj);
//                                }
//                            }
//                        }

//                    }
//                }

//                //string update_query = $@"IF EXISTS (SELECT 1 FROM dbo.netsuite_item_log where log_message = 'Shopee')
//                //BEGIN
//                //    UPDATE dbo.netsuite_item_log
//                //    SET last_run_time    = current_run_time,
//                //        current_run_time = '{dt}';
//                //END
//                //ELSE
//                //BEGIN
//                //    INSERT INTO dbo.netsuite_item_log (current_run_time, last_run_time,log_message)
//                //    VALUES ('{dt}', '{lastCheck}','Shopee');
//                //END";
//                //DataAccess.Select(conz, update_query);

//            }
//            catch (Exception ex)
//            {
//                // Log the exception (you can use your preferred logging framework)
//                throw new Exception($"Error syncing Shopee items: {ex.Message}");
//            }
//        }
//        public async Task<ShopeeApiResult> InitTierVariation(string payloadJson)
//        {


//            var token = await tokenService.CallRefreshEndpoint();
//            var accessToken = token.AccessToken;

//            var path = "/api/v2/product/init_tier_variation";
//            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
//            var sign = GenerateSignature(path, timestamp, accessToken);

//            var url = $"{_baseUrl}{path}?partner_id={_partnerId}&timestamp={timestamp}&sign={sign}&access_token={accessToken}&shop_id={_shopId}";

//            using (var client = new HttpClient())
//            using (var content = new StringContent(payloadJson, Encoding.UTF8, "application/json"))
//            {
//                var response = await client.PostAsync(url, content);
//                var body = await response.Content.ReadAsStringAsync();

//                return new ShopeeApiResult
//                {
//                    StatusCode = (int)response.StatusCode,
//                    IsSuccess = response.IsSuccessStatusCode && !body.Contains("\"error\""),
//                    ReasonPhrase = response.ReasonPhrase,
//                    Body = body
//                };
//            }


//        }
//        public async Task<ShopeeApiResult> UpdateTierVariation(string payloadJson)
//        {
//            var token = await tokenService.CallRefreshEndpoint();
//            var accessToken = token.AccessToken;

//            var path = "/api/v2/product/update_tier_variation";
//            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
//            var sign = GenerateSignature(path, timestamp, accessToken);

//            var url = $"{_baseUrl}{path}?partner_id={_partnerId}&timestamp={timestamp}&sign={sign}&access_token={accessToken}&shop_id={_shopId}";

//            using (var client = new HttpClient())
//            using (var content = new StringContent(payloadJson, Encoding.UTF8, "application/json"))
//            {
//                var response = await client.PostAsync(url, content);
//                var body = await response.Content.ReadAsStringAsync();

//                return new ShopeeApiResult
//                {
//                    StatusCode = (int)response.StatusCode,
//                    IsSuccess = response.IsSuccessStatusCode && !body.Contains("\"error\""),
//                    ReasonPhrase = response.ReasonPhrase,
//                    Body = body
//                };
//            }
//        }
//        public async Task<bool> SyncTierVariations(
//        string shopeeProductId,
//        List<TierVariationInput> newTiers,
//        JArray tierVariation)
//        {
//            foreach (var newTier in newTiers)
//            {
//                var existingTier = tierVariation
//                    .FirstOrDefault(t =>
//                        string.Equals((string)t["name"], newTier.Name, StringComparison.OrdinalIgnoreCase));
//                if (existingTier == null)
//                {
//                    var newTierIndex = newTiers.IndexOf(newTier);
//                    existingTier = tierVariation.ElementAtOrDefault(newTierIndex) as JObject;
//                }
//                if (existingTier == null) continue;
//                // UPDATE name if different
//                var existingName = (string)existingTier["name"];
//                if (!string.Equals(existingName, newTier.Name, StringComparison.OrdinalIgnoreCase))
//                {
//                    existingTier["name"] = newTier.Name;
//                }

//                var optionList = (JArray)existingTier["option_list"];

//                // REMOVE options that no longer exist in newTier
//                var newOptionNames = newTier.Options
//                    .Select(o => o.Option.ToLowerInvariant())
//                    .ToHashSet();

//                //var optionsToRemove = optionList
//                //    .Where(x => !newOptionNames.Contains(((string)x["option"]).ToLowerInvariant()))
//                //    .ToList();

//                //foreach (var opt in optionsToRemove)
//                //    optionList.Remove(opt);

//                // ADD or UPDATE existing options
//                foreach (var newOpt in newTier.Options)
//                {
//                    var existingOpt = optionList.FirstOrDefault(x =>
//                        string.Equals((string)x["option"], newOpt.Option, StringComparison.OrdinalIgnoreCase));

//                    if (existingOpt == null)
//                    {
//                        // ADD
//                        var newOptionObj = new JObject { ["option"] = newOpt.Option };

//                        if (!string.IsNullOrWhiteSpace(newOpt.ImageId))
//                            newOptionObj["image"] = new JObject { ["image_id"] = newOpt.ImageId };

//                        optionList.Add(newOptionObj);
//                    }
//                    else
//                    {
//                        // UPDATE image only if different
//                        var existingImageId = (string)existingOpt["image"]?["image_id"];

//                        if (!string.IsNullOrWhiteSpace(newOpt.ImageId) &&
//                            !string.Equals(existingImageId, newOpt.ImageId, StringComparison.Ordinal))
//                        {
//                            existingOpt["image"] = new JObject { ["image_id"] = newOpt.ImageId };
//                        }

//                        // CLEAR image if newOpt has no image but existing does
//                        else if (string.IsNullOrWhiteSpace(newOpt.ImageId) && existingOpt["image"] != null)
//                        {
//                            //existingOpt.Remove("image");
//                            existingOpt["image"] = null;
//                        }
//                    }
//                }
//            }

//            var payload = new JObject
//            {
//                ["item_id"] = Convert.ToInt64(shopeeProductId),
//                ["tier_variation"] = tierVariation
//            };

//            var result = await UpdateTierVariation(payload.ToString());
//            return result.StatusCode == 200;
//        }
//        private List<ModelInput> BuildModelInputs(List<NetsuiteItemModel> nsItems, JArray refreshedTiers)
//        {
//            var models = new List<ModelInput>();

//            // Get the option_list from the first (and possibly only) tier
//            var optionList = (JArray)refreshedTiers[0]["option_list"];

//            foreach (var item in nsItems)
//            {
//                // Normalize variation the same way we did for tier sync
//                string variation = string.IsNullOrEmpty((string)item.variation)
//                    ? (string)item.itemid
//                    : ((string)item.variation).Length > 25
//                        ? ((string)item.variation).Substring(0, 25)
//                        : (string)item.variation;

//                // Find its index in the refreshed option_list
//                int tierIndex = -1;
//                for (int i = 0; i < optionList.Count; i++)
//                {
//                    if (string.Equals((string)optionList[i]["option"], variation, StringComparison.OrdinalIgnoreCase))
//                    {
//                        tierIndex = i;
//                        break;
//                    }
//                }

//                if (tierIndex == -1) continue; // Couldn't match — skip

//                models.Add(new ModelInput
//                {
//                    TierIndex = new List<int> { tierIndex },
//                    Price = double.TryParse((string)item.sku_baseprice, out var price) ? (decimal)price : 0,
//                    Stock = int.TryParse(item.totalquantityonhand?.ToString(), out var stock) ? stock : 0,
//                    SellerSku = (string)item.itemid
//                });
//            }

//            return models;
//        }
//        public async Task<ShopeeApiResult> AddModel(string payloadJson)
//        {


//            var token = await tokenService.CallRefreshEndpoint();
//            var accessToken = token.AccessToken;

//            var path = "/api/v2/product/add_model";
//            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
//            var sign = GenerateSignature(path, timestamp, accessToken);

//            var url = $"{_baseUrl}{path}?partner_id={_partnerId}&timestamp={timestamp}&sign={sign}&access_token={accessToken}&shop_id={_shopId}";

//            using (var client = new HttpClient())
//            using (var content = new StringContent(payloadJson, Encoding.UTF8, "application/json"))
//            {
//                var response = await client.PostAsync(url, content);
//                var body = await response.Content.ReadAsStringAsync();

//                return new ShopeeApiResult
//                {
//                    StatusCode = (int)response.StatusCode,
//                    IsSuccess = response.IsSuccessStatusCode, //&& !body.Contains("\"error\""),
//                    ReasonPhrase = response.ReasonPhrase,
//                    Body = body
//                };
//            }


//        }
//        public async Task<bool> SyncModels(string shopeeProductId, List<ModelInput> newModels, JObject responseObj)
//        {
//            var existingModels = (JArray)responseObj["response"]?["model"] ?? new JArray();


//            var toAdd = new JArray();
//            var toUpdateModel = new JArray(); // SKU changes
//            var toUpdatePrice = new JArray(); // Price changes
//            var toUpdateStock = new JArray(); // Stock changes

//            foreach (var newModel in newModels)
//            {
//                var existing = existingModels.FirstOrDefault(m =>
//                {
//                    var idx = (JArray)m["tier_index"];
//                    if (idx == null || idx.Count != newModel.TierIndex.Count) return false;
//                    return newModel.TierIndex.Select((val, i) => (int)idx[i] == val).All(x => x);
//                });

//                if (existing == null)
//                {
//                    toAdd.Add(BuildModelObject(newModel));
//                }
//                else
//                {
//                    var modelId = (long)existing["model_id"];
//                    var existingPrice = (decimal?)existing["price_info"]?[0]?["current_price"] ?? 0;
//                    var existingStock = (int?)existing["stock_info"]?[0]?["current_stock"] ?? 0;

//                    // Price changed
//                    if (existingPrice != newModel.Price)
//                    {
//                        toUpdatePrice.Add(new JObject
//                        {
//                            ["model_id"] = modelId,
//                            ["original_price"] = newModel.Price
//                        });
//                    }

//                    // Stock changed
//                    if (existingStock != newModel.Stock)
//                    {
//                        toUpdateStock.Add(new JObject
//                        {
//                            ["model_id"] = modelId,
//                            ["stock"] = newModel.Stock
//                        });
//                    }
//                }
//            }

//            bool addSuccess = true;
//            bool updateModelSuccess = true;
//            bool updatePriceSuccess = true;
//            bool updateStockSuccess = true;

//            if (toAdd.Count > 0)
//            {
//                var addPayload = new JObject
//                {
//                    ["item_id"] = Convert.ToInt64(shopeeProductId),
//                    ["model_list"] = toAdd
//                };
//                var addResult = await AddModel(addPayload.ToString());
//                addSuccess = addResult.IsSuccess;
//            }
//            if (toUpdatePrice.Count > 0)
//            {
//                var pricePayload = new JObject
//                {
//                    ["item_id"] = Convert.ToInt64(shopeeProductId),
//                    ["price_list"] = toUpdatePrice
//                };
//                var priceResult = await UpdatePrice(pricePayload.ToString());
//                updatePriceSuccess = priceResult.IsSuccess;
//            }

//            if (toUpdateStock.Count > 0)
//            {
//                var stockPayload = new JObject
//                {
//                    ["item_id"] = Convert.ToInt64(shopeeProductId),
//                    ["stock_list"] = new JArray(toUpdateStock.Select(s => new JObject
//                    {
//                        ["model_id"] = s["model_id"],
//                        ["seller_stock"] = new JArray
//                        {
//                            new JObject
//                            {
//                                ["location_id"] = "PHZ",
//                                ["stock"] = s["stock"]
//                            }
//                        }
//                    }))
//                };
//                var stockResult = await UpdateStock(stockPayload.ToString());
//                updateStockSuccess = stockResult.IsSuccess;
//            }

//            return addSuccess && updatePriceSuccess && updateStockSuccess;
//        }
//        public async Task<ShopeeApiResult> GetModelList(string ProductId)
//        {


//            var token = await tokenService.CallRefreshEndpoint();
//            var accessToken = token.AccessToken;

//            var path = "/api/v2/product/get_model_list";
//            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
//            var sign = GenerateSignature(path, timestamp, accessToken);
//            var url = string.Format(
//                    "{0}{1}?partner_id={2}&timestamp={3}&sign={4}&access_token={5}&shop_id={6}&item_id={7}",
//                    _baseUrl,
//                    path,
//                    _partnerId,
//                    timestamp,
//                    sign,
//                    Uri.EscapeDataString(accessToken),
//                    _shopId,
//                    ProductId
//                );

//            using (var client = new HttpClient())
//            {
//                var response = await client.GetAsync(url);
//                var body = await response.Content.ReadAsStringAsync();

//                return new ShopeeApiResult
//                {
//                    StatusCode = (int)response.StatusCode,
//                    IsSuccess = response.IsSuccessStatusCode && !body.Contains("\"error\""),
//                    ReasonPhrase = response.ReasonPhrase,
//                    Body = body
//                };
//            }


//        }
//        public async Task<ShopeeApiResult> AddItem(string payloadJson)
//        {
//            const int maxRetries = 3;
//            string lastError = null;

//            for (int attempt = 1; attempt <= maxRetries; attempt++)
//            {
//                try
//                {
//                    var token = await tokenService.CallRefreshEndpoint();
//                    var accessToken = token.AccessToken;

//                    var path = "/api/v2/product/add_item";
//                    var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
//                    var sign = GenerateSignature(path, timestamp, accessToken);

//                    var url = $"{_baseUrl}{path}?partner_id={_partnerId}&timestamp={timestamp}&sign={sign}&access_token={accessToken}&shop_id={_shopId}";

//                    using (var client = new HttpClient())
//                    using (var content = new StringContent(payloadJson, Encoding.UTF8, "application/json"))
//                    {
//                        var response = await client.PostAsync(url, content);
//                        var body = await response.Content.ReadAsStringAsync();

//                        // Retry only if Shopee returns transient server error in body
//                        if ((int)response.StatusCode == 200 && body.Contains("product.error_server"))
//                        {
//                            lastError = body;

//                            if (attempt == maxRetries)
//                            {
//                                throw new Exception($"Shopee request failed after {maxRetries} attempts. Response: {body}");
//                            }

//                            await Task.Delay(1000 * attempt);
//                            continue;
//                        }

//                        return new ShopeeApiResult
//                        {
//                            StatusCode = (int)response.StatusCode,
//                            IsSuccess = response.IsSuccessStatusCode && !body.Contains("\"error\""),
//                            ReasonPhrase = response.ReasonPhrase,
//                            Body = body
//                        };
//                    }
//                }
//                catch (HttpRequestException ex)
//                {
//                    lastError = ex.Message;

//                    if (attempt == maxRetries)
//                    {
//                        throw new Exception($"AddItem HTTP error after {maxRetries} attempts: {ex.Message}", ex);
//                    }

//                    await Task.Delay(1000 * attempt);
//                }
//                catch (TaskCanceledException ex)
//                {
//                    lastError = ex.Message;

//                    if (attempt == maxRetries)
//                    {
//                        throw new Exception($"AddItem timeout after {maxRetries} attempts: {ex.Message}", ex);
//                    }

//                    await Task.Delay(1000 * attempt);
//                }
//                catch (Exception ex)
//                {
//                    throw new Exception($"AddItem error: {ex.Message}", ex);
//                }
//            }

//            throw new Exception($"AddItem failed after {maxRetries} attempts. Last error: {lastError}");
//        }

//        private string GenerateSignature(string path, long timestamp, string accessToken)
//        {
//            var raw = $"{_partnerId}{path}{timestamp}{accessToken}{_shopId}";
//            var keyBytes = Encoding.UTF8.GetBytes(_partnerKey);
//            var msgBytes = Encoding.UTF8.GetBytes(raw);

//            using (var hmac = new HMACSHA256(keyBytes))
//            {
//                var hashBytes = hmac.ComputeHash(msgBytes);
//                return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
//            }
//        }

//        private static string UrlEncode(string value)
//        {
//            return Uri.EscapeDataString(value ?? string.Empty)
//                      .Replace("!", "%21")
//                      .Replace("*", "%2A")
//                      .Replace("'", "%27")
//                      .Replace("(", "%28")
//                      .Replace(")", "%29");
//        }
//        private JObject BuildModelObject(ModelInput m)
//        {
//            var obj = new JObject
//            {
//                ["tier_index"] = new JArray(m.TierIndex),
//                ["model_sku"] = m.SellerSku,
//                ["original_price"] = m.Price,
//                ["seller_stock"] = new JArray
//        {
//            new JObject
//            {
//                ["location_id"] = "PHZ",
//                ["stock"]       = m.Stock
//            }
//        }
//            };

//            return obj;
//        }
//        public async Task<ShopeeApiResult> UpdateModel(string payloadJson)
//        {
//            var token = await tokenService.CallRefreshEndpoint();
//            var accessToken = token.AccessToken;

//            var path = "/api/v2/product/update_model";
//            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
//            var sign = GenerateSignature(path, timestamp, accessToken);

//            var url = $"{_baseUrl}{path}?partner_id={_partnerId}&timestamp={timestamp}&sign={sign}&access_token={accessToken}&shop_id={_shopId}";

//            using (var client = new HttpClient())
//            using (var content = new StringContent(payloadJson, Encoding.UTF8, "application/json"))
//            {
//                var response = await client.PostAsync(url, content);
//                var body = await response.Content.ReadAsStringAsync();

//                return new ShopeeApiResult
//                {
//                    StatusCode = (int)response.StatusCode,
//                    IsSuccess = response.IsSuccessStatusCode && !body.Contains("\"error\""),
//                    ReasonPhrase = response.ReasonPhrase,
//                    Body = body
//                };
//            }
//        }
//        public async Task<ShopeeApiResult> UpdatePrice(string payloadJson)
//        {
//            var token = await tokenService.CallRefreshEndpoint();
//            var accessToken = token.AccessToken;

//            var path = "/api/v2/product/update_price";
//            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
//            var sign = GenerateSignature(path, timestamp, accessToken);

//            var url = $"{_baseUrl}{path}?partner_id={_partnerId}&timestamp={timestamp}&sign={sign}&access_token={accessToken}&shop_id={_shopId}";

//            using (var client = new HttpClient())
//            using (var content = new StringContent(payloadJson, Encoding.UTF8, "application/json"))
//            {
//                var response = await client.PostAsync(url, content);
//                var body = await response.Content.ReadAsStringAsync();

//                return new ShopeeApiResult
//                {
//                    StatusCode = (int)response.StatusCode,
//                    IsSuccess = response.IsSuccessStatusCode && !body.Contains("\"error\""),
//                    ReasonPhrase = response.ReasonPhrase,
//                    Body = body
//                };
//            }
//        }

//        public async Task<ShopeeApiResult> UpdateStock(string payloadJson)
//        {
//            var token = await tokenService.CallRefreshEndpoint();
//            var accessToken = token.AccessToken;

//            var path = "/api/v2/product/update_stock";
//            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
//            var sign = GenerateSignature(path, timestamp, accessToken);

//            var url = $"{_baseUrl}{path}?partner_id={_partnerId}&timestamp={timestamp}&sign={sign}&access_token={accessToken}&shop_id={_shopId}";

//            using (var client = new HttpClient())
//            using (var content = new StringContent(payloadJson, Encoding.UTF8, "application/json"))
//            {
//                var response = await client.PostAsync(url, content);
//                var body = await response.Content.ReadAsStringAsync();

//                return new ShopeeApiResult
//                {
//                    StatusCode = (int)response.StatusCode,
//                    IsSuccess = response.IsSuccessStatusCode && !body.Contains("\"error\""),
//                    ReasonPhrase = response.ReasonPhrase,
//                    Body = body
//                };
//            }
//        }

//    }

//}
//public class ShopeeApiResult
//{
//    public int StatusCode { get; set; }
//    public bool IsSuccess { get; set; }
//    public string ReasonPhrase { get; set; }
//    public string Body { get; set; }
//}
//public class TierOptionInput
//{
//    public string Option { get; set; }
//    public string ImageId { get; set; } // nullable
//}

//public class TierVariationInput
//{
//    public string Name { get; set; }          // must match existing tier name
//    public List<TierOptionInput> Options { get; set; }
//}
//public class ModelInput
//{
//    public List<int> TierIndex { get; set; }   // e.g. [0] or [0, 1] for 1 or 2 tiers
//    public decimal Price { get; set; }
//    public int Stock { get; set; }
//    public string SellerSku { get; set; }       // nullable
//}