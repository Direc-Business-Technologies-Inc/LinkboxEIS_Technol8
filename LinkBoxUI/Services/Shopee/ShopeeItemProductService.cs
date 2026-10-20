using ClosedXML.Excel;
using DataAccessLayer.Class;
using DomainLayer.Models;
using LinkBoxUI.Context;
using LinkBoxUI.Helpers;
using LinkBoxUI.Services.Shopify;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NPOI.POIFS.Properties;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Helpers;

namespace LinkBoxUI.Services.Shopee
{
    public class ShopeeItemProductService
    {
        LinkboxDb _context = new LinkboxDb();
        NetsuiteHelper nsHelper = new NetsuiteHelper();
        ShopeeTokenService tokenService = new ShopeeTokenService();
        NsInventoryItemToShopifyService callJson = new NsInventoryItemToShopifyService();
        private static string _baseUrl =>
              ConfigurationManager.AppSettings["Sh_baseUrl"];
        private static string _partnerId =>
            ConfigurationManager.AppSettings["Sh_partnerId"];
        private static string _partnerKey =>
            ConfigurationManager.AppSettings["Sh_partnerKey"];
        private static string _shopId =>
            ConfigurationManager.AppSettings["Sh_shopId"];


        public async Task SyncShopeItems(string Code, DateTime dt)
        {
            var QueryEndpoint = _context.APISetups
                .Where(x => x.APICode.Contains("NSQueryItemData"))
                .Select(x => x.APIURL)
                .FirstOrDefault();

            string conz = ConfigurationManager.ConnectionStrings["SAOLinkBox"] != null
                ? ConfigurationManager.ConnectionStrings["SAOLinkBox"].ToString()
                : "";
            string query_log = $@"select top 1 last_run_time  from dbo.netsuite_item_log where log_message = 'Shopee'";

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
                                i.itemid,
                                i.displayname,
                                i.totalquantityonhand,
                                i.parent,
                                parent.itemid AS custitem_skucode,
                                parent.custitem_dbti_title,
                                -- parent.custitem_dbti_description,
platform_parent.custrecord_dbti_product_title AS custrecord_dbti_product_title,
								platform_parent.custrecord_dbti_product_title AS custitem_dbti_description,
                                platform_parent.custrecord_dbti_ecom_platid AS product_id,
                                p.custrecord_dbti_ecom_platid AS product_id_v2,
                                i.custitem_su_shpid AS shopee_id,
                                p.custrecord_dbti_ecom_variation AS variation,
                                p.custrecord_dbti_ecom_baseprice AS base_price,
                                p.custrecord_dbti_ecom_skuid AS variant_id,
                                platform_parent.custrecord_dbti_ecom_imagelink AS image_ids,
                                p.custrecord_dbti_ecom_imagelink AS child_image,
	                            p.custrecord_dbti_ecom_category_list as category
                                FROM item i
                                JOIN customrecord_dbti_platform p
                                    ON p.custrecord_dbti_ecom_item = i.id
                                JOIN item parent
                                    ON parent.id = i.parent
                                JOIN customrecord_dbti_platform platform_parent
                                    ON platform_parent.custrecord_dbti_ecom_item = parent.id
                                WHERE 1=1
                                AND i.custitem_dbti_ecom_reviseditem IS NOT NULL
                                AND i.custitem_dbti_ecom_reviseditem = 'T'
                                AND p.custrecord_dbti_ecom_customer = 213

                                AND platform_parent.custrecord_dbti_ecom_customer IN (213)

                                AND i.id IN (8218,8219)
                                AND (i.lastmodifieddate > TO_DATE('{lastCheckString}', 'YYYY-MM-DD HH24:MI:SS'))

                                ORDER BY
                                CASE
                                    WHEN i.parent IS NULL THEN 0
                                    ELSE 1
                                END,
                                i.parent";

                var nsInventoryItems = await nsHelper.GetNetsuiteJson(QueryEndpoint, query);

                if (nsInventoryItems.Count == 0)
                {
                    return;
                }

                var group_by_items = nsInventoryItems
                    .GroupBy(x => x.skucode)
                    .Select(g => new
                    {
                        ParentId = g.Key,
                        Items = g.GroupBy(x => x.itemid)
                                 .Select(x => x.First())
                                 .ToList()
                    })
                    .ToList();

                foreach (var items in group_by_items)
                {
                    var firstItem = items.Items.FirstOrDefault();
                    var ShopeeProduct_Id = firstItem.product_id;

                    if (string.IsNullOrEmpty(ShopeeProduct_Id)) // ── PATH 1: New Product ──
                    {
                        // ── Upload parent images (list) ──
                        var parentImageIds = await UploadImageFromNetsuite(firstItem.image_ids, firstItem.skucode);

                        if (parentImageIds == null || parentImageIds.Count == 0)
                        {
                            _context.SystemLogs.Add(new DomainLayer.Models.SystemLog
                            {
                                Task = "Upload Image - Shopee",
                                CreateDate = DateTime.Now,
                                Json = "",
                                ErrorMsg = "Parent Image ID is null",
                                Module = "Item Master Date"
                            });
                            _context.SaveChanges();
                            continue;
                        }

                        // ── Build new product payload ──
                        int categoryId = 101635;//Live
                        //int categoryId = 301635;//SB

                        if (!string.IsNullOrEmpty(firstItem.category))
                        {
                            var queryt = $"select name as id from customlist_dbti_ecom_category_list where id in ({firstItem.category})";
                            var categoryName = await nsHelper.GetNetSuiteData(QueryEndpoint, queryt);

                            if (!string.IsNullOrEmpty(categoryName))
                            {

                                categoryId = int.Parse(categoryName.Split('-')[0].Trim());
                            }

                        }


                        //var jsonObject = new
                        //{
                        //    item_name = firstItem.custitem_dbti_title,
                        //    item_sku = firstItem.skucode,
                        //    description = firstItem.custitem_dbti_description,
                        //    original_price = double.TryParse(firstItem.sku_baseprice, out var price) ? price : 0,
                        //    weight = double.TryParse(firstItem.weight, out var weight) && weight > 0 ? weight : 0.1,
                        //    category_id = categoryId,
                        //    brand = new
                        //    {
                        //        brand_id = 1704696,
                        //    },
                        //    image = new
                        //    {
                        //        image_id_list = parentImageIds.ToArray()
                        //    },
                        //    logistic_info = new[]
                        //    {
                        //    new { logistic_id = 41003, enabled = true, is_free = false, size_id = 0 },
                        //    new { logistic_id = 41004, enabled = true, is_free = false, size_id = 0 }
                        //},
                        //    seller_stock = new[]
                        //    {
                        //    new { location_id = "PHZ", stock = 0 }
                        //}
                        //};
                        var certExpiryDate = new DateTimeOffset(DateTime.UtcNow.Year + 5, 12, 31, 23, 59, 59, TimeSpan.Zero).ToUnixTimeSeconds();
                        string product_Desc = StripHtml(firstItem.custitem_dbti_description);
                        var jsonObject = new JsonObject
                        {
                            ["item_name"] = firstItem?.custitem_dbti_title ?? "Sample Item",
                            ["item_sku"] = firstItem?.skucode,
                            ["description"] = (product_Desc.Length > 5000
                                        ? product_Desc.Substring(0, 5000)
                                        : product_Desc),
                            ["original_price"] = double.TryParse(firstItem.sku_baseprice, out var price) ? price : 0,
                            ["weight"] = double.TryParse(firstItem?.weight, out var weight) && weight > 0 ? weight : 0.1,
                            ["category_id"] = categoryId,
                            ["brand"] = new JsonObject { ["brand_id"] = 1704696 },
                            ["item_status"] = "UNLIST",
                            ["certification_info"] = new JsonObject
                            {
                                ["certification_list"] = new JsonArray(
                                new JsonObject
                                {
                                    ["permit_id"] = 22,
                                    ["certification_no"] = "NN-000000000000",
                                    ["expiry_date"] = certExpiryDate,
                                    ["certification_proofs"] = new JsonArray(
                                        new JsonObject
                                        {
                                            ["image_id"] = "ph-11134208-820lg-mnm9a2elo7b77a"
                                        }
                                    )
                                }
                            )
                            },
                            //["image"] = new JsonObject
                            //{
                            //    ["image_id_list"] = new JsonArray(parentImageIds.Select(id => JsonValue.Create(id)).ToArray())
                            //},

                            //Live
                            ["logistic_info"] = new JsonArray(
                                    new JsonObject { ["logistic_id"] = 4000, ["enabled"] = true, ["is_free"] = false, ["size_id"] = 0 },
                                    new JsonObject { ["logistic_id"] = 4103, ["enabled"] = true, ["is_free"] = false, ["size_id"] = 0 }
                                ),

                            //SB
                            //["logistic_info"] = new JsonArray(
                            //        new JsonObject { ["logistic_id"] = 41003, ["enabled"] = true, ["is_free"] = false, ["size_id"] = 0 },
                            //        new JsonObject { ["logistic_id"] = 41004, ["enabled"] = true, ["is_free"] = false, ["size_id"] = 0 }
                            //    ),

                            ["seller_stock"] = new JsonArray(
                                    new JsonObject { ["location_id"] = "PHZ", ["stock"] = 0 }
                                ),
                            ["dimension"] = new JsonObject
                            {
                                ["package_length"] = 10,
                                ["package_width"] = 10,
                                ["package_height"] = 10
                            }
                        };

                        //Live
                        var categoryAttributes = new Dictionary<int, AttributeMapping>
                            {
                                { 101642, new AttributeMapping(100036, 738) },
                                { 101630, new AttributeMapping(100036, 738) },
                                { 100665, new AttributeMapping(100093, 760) },
                                { 100093, new AttributeMapping(100134, 1257) },
                            };


                        if (parentImageIds?.Count > 0)
                        {
                            jsonObject["image"] = new JsonObject
                            {
                                ["image_id_list"] = new JsonArray(
                                    parentImageIds.Select(id => JsonValue.Create(id)).ToArray()
                                )
                            };
                        }

                        if (categoryAttributes.TryGetValue(categoryId, out var attr))
                        {
                            jsonObject["attribute_list"] = new JsonArray(
                                new JsonObject
                                {
                                    ["attribute_id"] = attr.AttributeId,
                                    ["attribute_value_list"] = new JsonArray(
                                        new JsonObject { ["value_id"] = attr.ValueId }
                                    )
                                }
                            );
                        }


                        var _json = jsonObject.ToJsonString();


                        //string payload = JsonConvert.SerializeObject(_json, Formatting.Indented);
                        string payload = jsonObject.ToJsonString();
                        var posting_result = await AddItem(payload);
                        if (posting_result.StatusCode == 200 && posting_result.ReasonPhrase == "OK" && !posting_result.Body.Contains("Failed to create product"))
                        {
                            var json = JObject.Parse(posting_result.Body);
                            var itemId = json["response"]?["item_id"]?.Value<long>();

                            if (itemId.HasValue)
                            {
                                await callJson.JsonCallScript(firstItem.parent, 213, long.Parse(itemId.ToString()));

                                // ── Upload child image — take first image only for variant option ──
                                var variantImageIds = await UploadImageFromNetsuite(firstItem.child_image, firstItem.itemid);
                                var variantImageId = variantImageIds?.FirstOrDefault();

                                var optionObj = new JObject();
                                optionObj["option"] = string.IsNullOrEmpty(firstItem.variation)
                                    ? ""
                                    : (firstItem.variation.Length > 25
                                        ? firstItem.variation.Substring(0, 25)
                                        : firstItem.variation);

                                if (!string.IsNullOrEmpty(variantImageId))
                                    optionObj["image"] = new JObject { ["image_id"] = variantImageId };

                                var init_tierPayloadObject = new JObject
                                {
                                    ["item_id"] = itemId,
                                    ["tier_variation"] = new JArray
                                    {
                                        new JObject
                                        {
                                            ["name"] = "Variation",
                                            ["option_list"] = new JArray { optionObj }
                                        }
                                    },
                                    ["model"] = new JArray
                                    {
                                        new JObject
                                        {
                                            ["tier_index"] = new JArray { 0 },
                                            ["model_sku"] = firstItem.itemid,
                                            ["original_price"] = double.TryParse(firstItem.sku_baseprice, out var _price) ? _price : 0,
                                            ["seller_stock"] = new JArray
                                            {
                                                new JObject
                                                {
                                                    ["location_id"] = "PHZ",
                                                    ["stock"] = int.TryParse(firstItem.totalquantityonhand?.ToString(), out var stock) ? stock : 0
                                                }
                                            }
                                        }
                                    }
                                };

                                string tierPayload = init_tierPayloadObject.ToString();
                                var tierResult = await InitTierVariation(tierPayload);
                                var x_json = JObject.Parse(tierResult.Body);

                                var error = x_json["error"]?.ToString();
                                var message = x_json["message"]?.ToString();
                                if (string.IsNullOrEmpty(error) && string.IsNullOrEmpty(message))
                                {
                                    _context.PlatformItemTable.Add(new DomainLayer.Models.PlatformItemTable
                                    {
                                        ReferenceId = itemId.ToString(),
                                        Platform = "Shopee",
                                        ItemId = firstItem.id,
                                        variantId =firstItem.itemid,
                                        CreatedDate = DateTime.Now,
                                        UploadDate = DateTime.Now,
                                        Message = $"Seeded from NetSuite {DateTime.Now}"
                                    });
                                    _context.SaveChanges();
                                }
                            }
                        }
                    }
                    else // ── PATH 2: Existing Product ──
                    {
                        // ── Upload parent images (list) ──
                        var parentImageIds = await UploadImageFromNetsuite(firstItem.image_ids, firstItem.skucode);

                        // ── Update item basic info ──
                        var updateObject = new
                        {
                            item_id = long.Parse(ShopeeProduct_Id),
                            item_name = firstItem.custitem_dbti_title,
                            description = APITokenAuthorizer.ConvertHtmlToPlainText(firstItem.custitem_dbti_description),
                            original_price = double.TryParse(firstItem.sku_baseprice, out var updatePrice) ? updatePrice : 0,
                            image = parentImageIds != null && parentImageIds.Count > 0
                                ? new { image_id_list = parentImageIds.ToArray() }
                                : null
                        };

                        string updatePayload = JsonConvert.SerializeObject(updateObject, Formatting.Indented);
                        var updateResult = await UpdateItem(updatePayload);
                        Console.WriteLine($"[Shopee] UpdateItem result: {updateResult.Body}");

                        // ── Update tier variations and models ──
                        var modelList = await GetModelList(ShopeeProduct_Id);

                        if (modelList.StatusCode == 200 && modelList.ReasonPhrase.Contains("OK"))
                        {
                            var obj = JObject.Parse(modelList.Body);
                            var tierVariation = (JArray)obj["response"]?["tier_variation"];

                            if (tierVariation != null && tierVariation.Count > 0)
                            {
                                // ── Upload child images for each variant — take first image only for variant option ──
                                var optionsWithImages = new List<TierOptionInput>();
                                foreach (var i in items.Items)
                                {
                                    var childImageIds = await UploadImageFromNetsuite(i.child_image, i.itemid);
                                    var childImageId = childImageIds?.FirstOrDefault();
                                    optionsWithImages.Add(new TierOptionInput
                                    {
                                        Option = string.IsNullOrEmpty(i.variation)
                                            ? i.itemid
                                            : (i.variation.Length > 25
                                                ? i.variation.Substring(0, 25)
                                                : i.variation),
                                        ImageId = childImageId
                                    });
                                }

                                var newTiers = new List<TierVariationInput>
                                {
                                    new TierVariationInput
                                    {
                                        Name = "Variation",
                                        Options = optionsWithImages
                                    }
                                };

                                bool tierSynced = await SyncTierVariations(ShopeeProduct_Id, newTiers, tierVariation);

                                if (tierSynced)
                                {
                                    var refreshedModelList = await GetModelList(ShopeeProduct_Id);
                                    var refreshedObj = JObject.Parse(refreshedModelList.Body);
                                    var refreshedTiers = (JArray)refreshedObj["response"]?["tier_variation"];

                                    var newModels = BuildModelInputs(items.Items, refreshedTiers);
                                    await SyncModels(ShopeeProduct_Id, newModels, refreshedObj);
                                }
                            }
                        }
                    }
                }


                string update_query = $@"IF EXISTS (SELECT 1 FROM dbo.netsuite_item_log where log_message = 'Shopee')
                BEGIN
                    UPDATE dbo.netsuite_item_log
                    SET last_run_time    = current_run_time,
                        current_run_time = '{dt}'
                     where log_message = 'Shopee';
                END
                ELSE
                BEGIN
                    INSERT INTO dbo.netsuite_item_log (current_run_time, last_run_time,log_message)
                    VALUES ('{dt}', '{lastCheck}','Shopee');
                END";

                DataAccess.Select(conz, update_query);


            }
            catch (Exception ex)
            {
                throw new Exception($"Error syncing Shopee items: {ex.Message}");
            }
        }
        private static string StripHtml(string html)
        {
            if (string.IsNullOrWhiteSpace(html)) return string.Empty;

            // Remove HTML tags
            var text = System.Text.RegularExpressions.Regex.Replace(html, "<.*?>", " ",
                System.Text.RegularExpressions.RegexOptions.Singleline);

            // Decode HTML entities (&amp; &nbsp; &lt; etc.)
            text = System.Net.WebUtility.HtmlDecode(text);

            // Collapse whitespace
            text = System.Text.RegularExpressions.Regex.Replace(text, @"\s+", " ").Trim();

            return text;
        }

        public async Task<ShopeeApiResult> InitTierVariation(string payloadJson)
        {
            var token = await tokenService.CallRefreshEndpoint();
            var accessToken = token.AccessToken;

            var path = "/api/v2/product/init_tier_variation";
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var sign = GenerateSignature(path, timestamp, accessToken);
            var url = $"{_baseUrl}{path}?partner_id={_partnerId}&timestamp={timestamp}&sign={sign}&access_token={accessToken}&shop_id={_shopId}";

            using (var client = new HttpClient())
            using (var content = new StringContent(payloadJson, Encoding.UTF8, "application/json"))
            {
                var response = await client.PostAsync(url, content);
                var body = await response.Content.ReadAsStringAsync();

                return new ShopeeApiResult
                {
                    StatusCode = (int)response.StatusCode,
                    IsSuccess = response.IsSuccessStatusCode && !body.Contains("\"error\""),
                    ReasonPhrase = response.ReasonPhrase,
                    Body = body
                };
            }
        }

        public async Task<ShopeeApiResult> UpdateTierVariation(string payloadJson)
        {
            var token = await tokenService.CallRefreshEndpoint();
            var accessToken = token.AccessToken;

            var path = "/api/v2/product/update_tier_variation";
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var sign = GenerateSignature(path, timestamp, accessToken);
            var url = $"{_baseUrl}{path}?partner_id={_partnerId}&timestamp={timestamp}&sign={sign}&access_token={accessToken}&shop_id={_shopId}";

            using (var client = new HttpClient())
            using (var content = new StringContent(payloadJson, Encoding.UTF8, "application/json"))
            {
                var response = await client.PostAsync(url, content);
                var body = await response.Content.ReadAsStringAsync();

                return new ShopeeApiResult
                {
                    StatusCode = (int)response.StatusCode,
                    IsSuccess = response.IsSuccessStatusCode && !body.Contains("\"error\""),
                    ReasonPhrase = response.ReasonPhrase,
                    Body = body
                };
            }
        }

        public async Task<ShopeeApiResult> UpdateItem(string payloadJson)
        {
            var token = await tokenService.CallRefreshEndpoint();
            var accessToken = token.AccessToken;

            var path = "/api/v2/product/update_item";
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var sign = GenerateSignature(path, timestamp, accessToken);
            var url = $"{_baseUrl}{path}?partner_id={_partnerId}&timestamp={timestamp}&sign={sign}&access_token={accessToken}&shop_id={_shopId}";

            using (var client = new HttpClient())
            using (var content = new StringContent(payloadJson, Encoding.UTF8, "application/json"))
            {
                var response = await client.PostAsync(url, content);
                var body = await response.Content.ReadAsStringAsync();

                return new ShopeeApiResult
                {
                    StatusCode = (int)response.StatusCode,
                    IsSuccess = response.IsSuccessStatusCode && !body.Contains("\"error\""),
                    ReasonPhrase = response.ReasonPhrase,
                    Body = body
                };
            }
        }

        public async Task<bool> SyncTierVariations(
            string shopeeProductId,
            List<TierVariationInput> newTiers,
            JArray tierVariation)
        {
            foreach (var newTier in newTiers)
            {
                var existingTier = tierVariation
                    .FirstOrDefault(t =>
                        string.Equals((string)t["name"], newTier.Name, StringComparison.OrdinalIgnoreCase));

                if (existingTier == null)
                {
                    var newTierIndex = newTiers.IndexOf(newTier);
                    existingTier = tierVariation.ElementAtOrDefault(newTierIndex) as JObject;
                }
                if (existingTier == null) continue;

                var existingName = (string)existingTier["name"];
                if (!string.Equals(existingName, newTier.Name, StringComparison.OrdinalIgnoreCase))
                    existingTier["name"] = newTier.Name;

                var optionList = (JArray)existingTier["option_list"];

                var newOptionNames = newTier.Options
                    .Select(o => o.Option.ToLowerInvariant())
                    .ToHashSet();

                foreach (var newOpt in newTier.Options)
                {
                    var existingOpt = optionList.FirstOrDefault(x =>
                        string.Equals((string)x["option"], newOpt.Option, StringComparison.OrdinalIgnoreCase));

                    if (existingOpt == null)
                    {
                        var newOptionObj = new JObject { ["option"] = newOpt.Option };
                        if (!string.IsNullOrWhiteSpace(newOpt.ImageId))
                            newOptionObj["image"] = new JObject { ["image_id"] = newOpt.ImageId };
                        optionList.Add(newOptionObj);
                    }
                    else
                    {
                        var existingImageId = (string)existingOpt["image"]?["image_id"];

                        if (!string.IsNullOrWhiteSpace(newOpt.ImageId) &&
                            !string.Equals(existingImageId, newOpt.ImageId, StringComparison.Ordinal))
                        {
                            existingOpt["image"] = new JObject { ["image_id"] = newOpt.ImageId };
                        }
                        else if (string.IsNullOrWhiteSpace(newOpt.ImageId) && existingOpt["image"] != null)
                        {
                            existingOpt["image"] = null;
                        }
                    }
                }
            }

            var payload = new JObject
            {
                ["item_id"] = Convert.ToInt64(shopeeProductId),
                ["tier_variation"] = tierVariation
            };

            var result = await UpdateTierVariation(payload.ToString());
            return result.StatusCode == 200;
        }

        private List<ModelInput> BuildModelInputs(List<NetsuiteItemModel> nsItems, JArray refreshedTiers)
        {
            var models = new List<ModelInput>();
            var optionList = (JArray)refreshedTiers[0]["option_list"];

            foreach (var item in nsItems)
            {
                string variation = string.IsNullOrEmpty((string)item.variation)
                    ? (string)item.itemid
                    : ((string)item.variation).Length > 25
                        ? ((string)item.variation).Substring(0, 25)
                        : (string)item.variation;

                int tierIndex = -1;
                for (int i = 0; i < optionList.Count; i++)
                {
                    if (string.Equals((string)optionList[i]["option"], variation, StringComparison.OrdinalIgnoreCase))
                    {
                        tierIndex = i;
                        break;
                    }
                }

                if (tierIndex == -1) continue;

                models.Add(new ModelInput
                {
                    TierIndex = new List<int> { tierIndex },
                    Price = double.TryParse((string)item.sku_baseprice, out var price) ? (decimal)price : 0,
                    Stock = int.TryParse(item.totalquantityonhand?.ToString(), out var stock) ? stock : 0,
                    SellerSku = (string)item.itemid
                });
            }

            return models;
        }

        public async Task<ShopeeApiResult> AddModel(string payloadJson)
        {
            var token = await tokenService.CallRefreshEndpoint();
            var accessToken = token.AccessToken;

            var path = "/api/v2/product/add_model";
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var sign = GenerateSignature(path, timestamp, accessToken);
            var url = $"{_baseUrl}{path}?partner_id={_partnerId}&timestamp={timestamp}&sign={sign}&access_token={accessToken}&shop_id={_shopId}";

            using (var client = new HttpClient())
            using (var content = new StringContent(payloadJson, Encoding.UTF8, "application/json"))
            {
                var response = await client.PostAsync(url, content);
                var body = await response.Content.ReadAsStringAsync();

                return new ShopeeApiResult
                {
                    StatusCode = (int)response.StatusCode,
                    IsSuccess = response.IsSuccessStatusCode,
                    ReasonPhrase = response.ReasonPhrase,
                    Body = body
                };
            }
        }

        public async Task<bool> SyncModels(string shopeeProductId, List<ModelInput> newModels, JObject responseObj)
        {
            var existingModels = (JArray)responseObj["response"]?["model"] ?? new JArray();

            var toAdd = new JArray();
            var toUpdatePrice = new JArray();
            var toUpdateStock = new JArray();

            foreach (var newModel in newModels)
            {
                var existing = existingModels.FirstOrDefault(m =>
                {
                    var idx = (JArray)m["tier_index"];
                    if (idx == null || idx.Count != newModel.TierIndex.Count) return false;
                    return newModel.TierIndex.Select((val, i) => (int)idx[i] == val).All(x => x);
                });

                if (existing == null)
                {
                    toAdd.Add(BuildModelObject(newModel));
                }
                else
                {
                    var modelId = (long)existing["model_id"];
                    var existingPrice = (decimal?)existing["price_info"]?[0]?["current_price"] ?? 0;
                    var existingStock = (int?)existing["stock_info"]?[0]?["current_stock"] ?? 0;

                    if (existingPrice != newModel.Price)
                    {
                        toUpdatePrice.Add(new JObject
                        {
                            ["model_id"] = modelId,
                            ["original_price"] = newModel.Price
                        });
                    }

                    if (existingStock != newModel.Stock)
                    {
                        toUpdateStock.Add(new JObject
                        {
                            ["model_id"] = modelId,
                            ["stock"] = newModel.Stock
                        });
                    }
                }
            }

            bool addSuccess = true;
            bool updatePriceSuccess = true;
            bool updateStockSuccess = true;

            if (toAdd.Count > 0)
            {
                var addPayload = new JObject
                {
                    ["item_id"] = Convert.ToInt64(shopeeProductId),
                    ["model_list"] = toAdd
                };
                var addResult = await AddModel(addPayload.ToString());
                addSuccess = addResult.IsSuccess;
            }

            if (toUpdatePrice.Count > 0)
            {
                var pricePayload = new JObject
                {
                    ["item_id"] = Convert.ToInt64(shopeeProductId),
                    ["price_list"] = toUpdatePrice
                };
                var priceResult = await UpdatePrice(pricePayload.ToString());
                updatePriceSuccess = priceResult.IsSuccess;
            }

            if (toUpdateStock.Count > 0)
            {
                var stockPayload = new JObject
                {
                    ["item_id"] = Convert.ToInt64(shopeeProductId),
                    ["stock_list"] = new JArray(toUpdateStock.Select(s => new JObject
                    {
                        ["model_id"] = s["model_id"],
                        ["seller_stock"] = new JArray
                        {
                            new JObject
                            {
                                ["location_id"] = "PHZ",
                                ["stock"] = s["stock"]
                            }
                        }
                    }))
                };
                var stockResult = await UpdateStock(stockPayload.ToString());
                updateStockSuccess = stockResult.IsSuccess;
            }

            return addSuccess && updatePriceSuccess && updateStockSuccess;
        }

        public async Task<ShopeeApiResult> GetModelList(string ProductId)
        {
            var token = await tokenService.CallRefreshEndpoint();
            var accessToken = token.AccessToken;

            var path = "/api/v2/product/get_model_list";
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var sign = GenerateSignature(path, timestamp, accessToken);
            var url = string.Format(
                "{0}{1}?partner_id={2}&timestamp={3}&sign={4}&access_token={5}&shop_id={6}&item_id={7}",
                _baseUrl, path, _partnerId, timestamp, sign,
                Uri.EscapeDataString(accessToken), _shopId, ProductId);

            using (var client = new HttpClient())
            {
                var response = await client.GetAsync(url);
                var body = await response.Content.ReadAsStringAsync();

                return new ShopeeApiResult
                {
                    StatusCode = (int)response.StatusCode,
                    IsSuccess = response.IsSuccessStatusCode && !body.Contains("\"error\""),
                    ReasonPhrase = response.ReasonPhrase,
                    Body = body
                };
            }
        }

        public async Task<ShopeeApiResult> AddItem(string payloadJson)
        {
            const int maxRetries = 3;
            string lastError = null;

            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                try
                {
                    var token = await tokenService.CallRefreshEndpoint();
                    var accessToken = token.AccessToken;

                    var path = "/api/v2/product/add_item";
                    var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                    var sign = GenerateSignature(path, timestamp, accessToken);
                    var url = $"{_baseUrl}{path}?partner_id={_partnerId}&timestamp={timestamp}&sign={sign}&access_token={accessToken}&shop_id={_shopId}";

                    using (var client = new HttpClient())
                    using (var content = new StringContent(payloadJson, Encoding.UTF8, "application/json"))
                    {
                        var response = await client.PostAsync(url, content);
                        var body = await response.Content.ReadAsStringAsync();

                        if ((int)response.StatusCode == 200 && body.Contains("product.error_server"))
                        {
                            lastError = body;
                            if (attempt == maxRetries)
                                throw new Exception($"Shopee request failed after {maxRetries} attempts. Response: {body}");
                            await Task.Delay(1000 * attempt);
                            continue;
                        }

                        return new ShopeeApiResult
                        {
                            StatusCode = (int)response.StatusCode,
                            IsSuccess = response.IsSuccessStatusCode && !body.Contains("\"error\""),
                            ReasonPhrase = response.ReasonPhrase,
                            Body = body
                        };
                    }
                }
                catch (HttpRequestException ex)
                {
                    lastError = ex.Message;
                    if (attempt == maxRetries)
                        throw new Exception($"AddItem HTTP error after {maxRetries} attempts: {ex.Message}", ex);
                    await Task.Delay(1000 * attempt);
                }
                catch (TaskCanceledException ex)
                {
                    lastError = ex.Message;
                    if (attempt == maxRetries)
                        throw new Exception($"AddItem timeout after {maxRetries} attempts: {ex.Message}", ex);
                    await Task.Delay(1000 * attempt);
                }
                catch (Exception ex)
                {
                    throw new Exception($"AddItem error: {ex.Message}", ex);
                }
            }

            throw new Exception($"AddItem failed after {maxRetries} attempts. Last error: {lastError}");
        }

        private async Task<List<string>> UploadImageFromNetsuite(string imageIdsRaw, string sellerSku)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(imageIdsRaw)) return null;

                var matches = Regex.Matches(imageIdsRaw, @"\d+");
                if (matches.Count == 0) return null;

                var imageIds = new List<string>();

                foreach (Match match in matches)
                {
                    var fileId = match.Value;

                    var existing = _context.FileCabinetImages
                        .FirstOrDefault(x => x.FileID == fileId
                                          && x.SKUFolder == sellerSku
                                          && x.Platform == "Shopee");

                    if (existing != null && !string.IsNullOrEmpty(existing.status))
                    {
                        Console.WriteLine($"[Shopee] Reusing cached image_id for {sellerSku}: {existing.status}");
                        imageIds.Add(existing.status);
                        continue;
                    }

                    var imageDetails = await GetImageDetails(fileId);
                    if (imageDetails == null || !imageDetails.success) continue;
                    if (string.IsNullOrEmpty(imageDetails.base64)) continue;

                    var imageBytes = Convert.FromBase64String(imageDetails.base64);
                    var imageName = imageDetails.name ?? $"{sellerSku}_{fileId}.jpg";

                    var imageId = await UploadImageToShopee(imageBytes, imageName);
                    if (string.IsNullOrEmpty(imageId)) continue;

                    if (existing == null)
                    {
                        _context.FileCabinetImages.Add(new DomainLayer.Models.FileCabinetImage
                        {
                            SKUFolder = sellerSku,
                            FileID = fileId,
                            Platform = "Shopee",
                            ImageName = imageName,
                            ImageBinary = imageBytes,
                            CreatedDate = DateTime.Now,
                            status = imageId
                        });
                    }
                    else
                    {
                        existing.status = imageId;
                        existing.UpdatedDate = DateTime.Now;
                        existing.ImageBinary = imageBytes;
                    }

                    _context.SaveChanges();
                    Console.WriteLine($"[Shopee] Uploaded image_id: {imageId} for {sellerSku}");
                    imageIds.Add(imageId);
                }

                return imageIds.Count > 0 ? imageIds : null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Shopee] UploadImageFromNetsuite error: {ex.Message}");
                _context.SystemLogs.Add(new DomainLayer.Models.SystemLog
                {
                    Task = "ShopeeItemProductService - UploadImageFromNetsuite",
                    ErrorMsg = ex.Message,
                    CreateDate = DateTime.Now,
                });
                _context.SaveChanges();
                return null;
            }
        }

        private async Task<string> UploadImageToShopee(byte[] imageBytes, string imageName)
        {
            var token = await tokenService.CallRefreshEndpoint();
            var accessToken = token.AccessToken;

            var path = "/api/v2/media_space/upload_image";
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var sign = GenerateSignature(path, timestamp, accessToken);
            var url = $"{_baseUrl}{path}?partner_id={_partnerId}&timestamp={timestamp}&sign={sign}&access_token={accessToken}&shop_id={_shopId}";

            using (var client = new HttpClient())
            using (var form = new MultipartFormDataContent())
            {
                var imageContent = new ByteArrayContent(imageBytes);
                imageContent.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
                form.Add(imageContent, "image", imageName);

                var response = await client.PostAsync(url, form);
                var body = await response.Content.ReadAsStringAsync();

                Console.WriteLine($"[Shopee] Upload image response: {body}");
                if (!response.IsSuccessStatusCode) return null;

                var json = JObject.Parse(body);
                return json["response"]?["image_info"]?["image_id"]?.Value<string>();
            }
        }

        private async Task<ImageDetailsResponse> GetImageDetails(string fileId)
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
                { "deploy", "1" },
                { "fileId", fileId }
            };

            var oauthParams = new Dictionary<string, string>
            {
                { "oauth_consumer_key", consumerKey },
                { "oauth_token", tokenId },
                { "oauth_nonce", Guid.NewGuid().ToString("N") },
                { "oauth_timestamp", ((int)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds).ToString() },
                { "oauth_signature_method", "HMAC-SHA256" },
                { "oauth_version", "1.0" }
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

                if (!response.IsSuccessStatusCode) return null;
                return JsonConvert.DeserializeObject<ImageDetailsResponse>(body);
            }
        }

        private string GenerateSignature(string path, long timestamp, string accessToken)
        {
            var raw = $"{_partnerId}{path}{timestamp}{accessToken}{_shopId}";
            var keyBytes = Encoding.UTF8.GetBytes(_partnerKey);
            var msgBytes = Encoding.UTF8.GetBytes(raw);

            using (var hmac = new HMACSHA256(keyBytes))
            {
                var hashBytes = hmac.ComputeHash(msgBytes);
                return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
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

        private JObject BuildModelObject(ModelInput m)
        {
            return new JObject
            {
                ["tier_index"] = new JArray(m.TierIndex),
                ["model_sku"] = m.SellerSku,
                ["original_price"] = m.Price,
                ["seller_stock"] = new JArray
                {
                    new JObject
                    {
                        ["location_id"] = "PHZ",
                        ["stock"] = m.Stock
                    }
                }
            };
        }

        public async Task<ShopeeApiResult> UpdateModel(string payloadJson)
        {
            var token = await tokenService.CallRefreshEndpoint();
            var accessToken = token.AccessToken;

            var path = "/api/v2/product/update_model";
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var sign = GenerateSignature(path, timestamp, accessToken);
            var url = $"{_baseUrl}{path}?partner_id={_partnerId}&timestamp={timestamp}&sign={sign}&access_token={accessToken}&shop_id={_shopId}";

            using (var client = new HttpClient())
            using (var content = new StringContent(payloadJson, Encoding.UTF8, "application/json"))
            {
                var response = await client.PostAsync(url, content);
                var body = await response.Content.ReadAsStringAsync();

                return new ShopeeApiResult
                {
                    StatusCode = (int)response.StatusCode,
                    IsSuccess = response.IsSuccessStatusCode && !body.Contains("\"error\""),
                    ReasonPhrase = response.ReasonPhrase,
                    Body = body
                };
            }
        }

        public async Task<ShopeeApiResult> UpdatePrice(string payloadJson)
        {
            var token = await tokenService.CallRefreshEndpoint();
            var accessToken = token.AccessToken;

            var path = "/api/v2/product/update_price";
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var sign = GenerateSignature(path, timestamp, accessToken);
            var url = $"{_baseUrl}{path}?partner_id={_partnerId}&timestamp={timestamp}&sign={sign}&access_token={accessToken}&shop_id={_shopId}";

            using (var client = new HttpClient())
            using (var content = new StringContent(payloadJson, Encoding.UTF8, "application/json"))
            {
                var response = await client.PostAsync(url, content);
                var body = await response.Content.ReadAsStringAsync();

                return new ShopeeApiResult
                {
                    StatusCode = (int)response.StatusCode,
                    IsSuccess = response.IsSuccessStatusCode && !body.Contains("\"error\""),
                    ReasonPhrase = response.ReasonPhrase,
                    Body = body
                };
            }
        }

        public async Task<ShopeeApiResult> UpdateStock(string payloadJson)
        {
            var token = await tokenService.CallRefreshEndpoint();
            var accessToken = token.AccessToken;

            var path = "/api/v2/product/update_stock";
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var sign = GenerateSignature(path, timestamp, accessToken);
            var url = $"{_baseUrl}{path}?partner_id={_partnerId}&timestamp={timestamp}&sign={sign}&access_token={accessToken}&shop_id={_shopId}";

            using (var client = new HttpClient())
            using (var content = new StringContent(payloadJson, Encoding.UTF8, "application/json"))
            {
                var response = await client.PostAsync(url, content);
                var body = await response.Content.ReadAsStringAsync();

                return new ShopeeApiResult
                {
                    StatusCode = (int)response.StatusCode,
                    IsSuccess = response.IsSuccessStatusCode && !body.Contains("\"error\""),
                    ReasonPhrase = response.ReasonPhrase,
                    Body = body
                };
            }
        }
    }
}

public class ShopeeApiResult
{
    public int StatusCode { get; set; }
    public bool IsSuccess { get; set; }
    public string ReasonPhrase { get; set; }
    public string Body { get; set; }
}

public class TierOptionInput
{
    public string Option { get; set; }
    public string ImageId { get; set; }
}

public class TierVariationInput
{
    public string Name { get; set; }
    public List<TierOptionInput> Options { get; set; }
}

public class ModelInput
{
    public List<int> TierIndex { get; set; }
    public decimal Price { get; set; }
    public int Stock { get; set; }
    public string SellerSku { get; set; }
}

public class ImageDetailsResponse
{
    public bool success { get; set; }
    public string base64 { get; set; }
    public string name { get; set; }
    public string type { get; set; }
}
public struct AttributeMapping
{
    public int AttributeId { get; }
    public int ValueId { get; }

    public AttributeMapping(int attributeId, int valueId)
    {
        AttributeId = attributeId;
        ValueId = valueId;
    }
}