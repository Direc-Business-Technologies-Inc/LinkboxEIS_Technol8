
using ClosedXML.Excel;
using DataAccessLayer.Class;
using DocumentFormat.OpenXml.Math;
using DocumentFormat.OpenXml.Office2010.Excel;
using DocumentFormat.OpenXml.Spreadsheet;
using DomainLayer.Models;
using DomainLayer.Models.Shopify;
using DomainLayer.ViewModels;
using LinkBoxUI.Context;
using LinkBoxUI.Helpers;
using Microsoft.DotNet.PlatformAbstractions;
using Microsoft.Graph.Models;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Buffers.Text;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.Entity;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using System.Web.Helpers;
using System.Web.Http;
using System.Web.Security;

namespace LinkBoxUI.Services.Shopify
{
    public  class NsInventoryItemToShopifyService
    {
        NetsuiteHelper nsHelper = new NetsuiteHelper();
        LinkboxDb _context = new LinkboxDb();
        ShopifyHelper shopifyHelper = new ShopifyHelper();
        NetsuiteHelperMethod nsHelperMethods = new NetsuiteHelperMethod();

        public async Task GetNsInventoryItemToShopify(int[] Mapid, DateTime dt)
        {
          
            var QueryEndpoint = _context.APISetups.Where(x => x.APICode.Contains("NSQueryItemData")).Select(x => x.APIURL).FirstOrDefault();

            string conz = ConfigurationManager.ConnectionStrings["SAOLinkBox"] != null ? ConfigurationManager.ConnectionStrings["SAOLinkBox"].ToString() : "";
            string query_log = $@"select top 1 last_run_time  from dbo.netsuite_item_log where log_message = 'Shopify'";


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


            foreach (var mapid in Mapid)
            {
                try
                {
                    

                    var query = $@"SELECT
                                    i.*,
            	                p.custrecord_dbti_ecom_platid as product_id,
				                p.custrecord_dbti_ecom_baseprice as base_price,
				                p.custrecord_dbti_ecom_skuid as variant_id,
                                CASE
                                        WHEN i.parent IS NOT NULL THEN pp.custrecord_dbti_product_title
                                        ELSE NULL
                                    END AS custrecord_dbti_product_title
                                FROM item i
                                JOIN customrecord_dbti_platform p
                                    ON p.custrecord_dbti_ecom_item = i.id
                                LEFT JOIN customrecord_dbti_platform pp
                                    ON pp.custrecord_dbti_ecom_item = i.parent
                                   AND pp.custrecord_dbti_ecom_customer = 348
                                WHERE  1=1
                                AND i.custitem_dbti_ecom_reviseditem IS NOT NULL
                                AND i.custitem_dbti_ecom_reviseditem = 'T'
                                AND p.custrecord_dbti_ecom_customer  = 348
                                AND i.id in (8217,8218,8219)
                                --AND (i.lastmodifieddate > TO_DATE('{lastCheckString}', 'YYYY-MM-DD HH24:MI:SS'))
                                ORDER BY
                                CASE 
                                    WHEN i.parent IS NULL THEN 0 
                                    ELSE 1  
                                END,
                                i.parent
                               
                                ";

                    var nsInventoryItems = await nsHelper.GetNetsuiteJson(QueryEndpoint, query);

                    if(nsInventoryItems.Count == 0)
                    {
                        return;
                    }

                    foreach(var netsuiteItem in nsInventoryItems)
                    {
                        var description = nsInventoryItems
                              .FirstOrDefault(x => !string.IsNullOrWhiteSpace(x.custrecord_dbti_product_title))
                              ?.custrecord_dbti_product_title?.ToString() ?? "";
                        var item_title = netsuiteItem.custitem_dbti_title?.ToString() ?? "";
                        var item_desc= description ?? "";

                        // var item_desc= netsuiteItem.custitem_dbti_description?.ToString() ?? "";

                        if (string.IsNullOrEmpty(netsuiteItem.parent))
                        {
                            //MASTER
                            if (string.IsNullOrEmpty(netsuiteItem.product_id))//check the master's product id is exist
                            {
                                var cmd_get_children = $@"
                                    SELECT
                                    p.custrecord_dbti_ecom_platid AS product_id
                                FROM item i
                                JOIN customrecord_dbti_platform p
                                    ON p.custrecord_dbti_ecom_item = i.id
                                WHERE
                                    p.custrecord_dbti_ecom_customer = 348
                                    AND p.custrecord_dbti_ecom_item = {netsuiteItem.id};";


                                var child_items = await nsHelper.GetNetsuiteJson(QueryEndpoint, cmd_get_children);

                                var firstValidProductId = child_items
                                .FirstOrDefault(x => x.product_id != null);

                                if (firstValidProductId != null)
                                {
                                    netsuiteItem.product_id = firstValidProductId.product_id;
                                }

                                //build json for posting to shopify


                                var Master_isexsit = _context.PlatformItemTable.Where(x => x.ItemId == netsuiteItem.id && x.Platform == "Shopify").FirstOrDefault();

                                if (Master_isexsit != null)
                                {
                                    var updateMasterData =
                                       await JsonCallScript(netsuiteItem.id, 348, long.Parse(Master_isexsit.ReferenceId));
                                    if (!updateMasterData.IsSuccess)
                                    {
                                        _context.SystemLogs.Add(new DomainLayer.Models.SystemLog
                                        {
                                            Task = $"151 : Error - Update Master Item with ProductId Script Call",
                                            ApiUrl = "JsonCallScript",
                                            Json = $"ItemId: {netsuiteItem.id}, ProductId: {Master_isexsit.ReferenceId}",
                                            ErrorMsg = $"Error {updateMasterData.StatusCode}: {updateMasterData.ReasonPhrase} - {updateMasterData.Body}",
                                            CreateDate = DateTime.Now,
                                        });
                                        _context.SaveChanges();
                                    }

                                    continue;

                                }
                             

                                    var PostMasTerData = new
                                    {
                                        product = new
                                        {
                                            title = item_title,
                                            body_html = item_desc,
                                            status = "draft",
                                            inventory_management = "shopify"
                                        }
                                    };
                                    long productId = 0;

                                if (!string.IsNullOrEmpty(item_title) && !string.IsNullOrEmpty(item_desc))
                                {
                                    var Parent_Posting = await PostItemHeaderToShopify(PostMasTerData);

                                    

                                    using (var doc = JsonDocument.Parse(Parent_Posting.Body.ToString()))
                                    {
                                        var root = doc.RootElement;

                                        // Shopify returns "product", not "products"
                                        if (!root.TryGetProperty("product", out var productElement))
                                            throw new InvalidOperationException("Shopify response does not contain 'product'");

                                        // Get product ID
                                        if (!productElement.TryGetProperty("id", out var idElement))
                                            throw new InvalidOperationException("Shopify product does not contain 'id'");

                                        productId = idElement.GetInt64();
                                    }
                                



                                var check_addon_db = _context.PlatformItemTable.Where(x => x.Platform == "Shopify" && x.ReferenceId == netsuiteItem.product_id.ToString() && x.ItemId == netsuiteItem.id).FirstOrDefault();
                                if (check_addon_db != null)
                                {
                                    check_addon_db.BuildJson = JsonConvert.SerializeObject(PostMasTerData);
                                    check_addon_db.UploadDate = DateTime.Now;
                                    check_addon_db.Message = $"Master Item Updated {DateTime.Now}";
                                }
                                else
                                {
                                        _context.PlatformItemTable.Add(new PlatformItemTable
                                        {
                                            ReferenceId = productId.ToString(),
                                            Platform = "Shopify",
                                            PlatformJson = Parent_Posting.Body.ToString(),
                                            CreatedDate = DateTime.Now,
                                            ItemId = netsuiteItem.id.ToString(),
                                            BuildJson = JsonConvert.SerializeObject(PostMasTerData),
                                            UploadDate = DateTime.Now,
                                            Message = $"Master Item Processed {DateTime.Now}"
                                        });


                                }
                                _context.SaveChanges();

                                if(firstValidProductId == null)
                                {
                                    var updateMasterData =
                                       await JsonCallScript(netsuiteItem.id, 348, productId);
                                    if (!updateMasterData.IsSuccess)
                                    {
                                        _context.SystemLogs.Add(new DomainLayer.Models.SystemLog
                                        {
                                            Task = $"Error - Update Master Item with ProductId Script Call",
                                            ApiUrl = "JsonCallScript",
                                            Json = $"ItemId: {netsuiteItem.id}, ProductId: {productId}",
                                            ErrorMsg = $"Error {updateMasterData.StatusCode}: {updateMasterData.ReasonPhrase} - {updateMasterData.Body}",
                                            CreateDate = DateTime.Now,
                                        });
                                        _context.SaveChanges();
                                    }
                                }


                                    ///Need to Add Conde here 
                                    if (productId != 0)
                                    {
                                        await SyncHeaderImageToShopify(netsuiteItem.id,productId,netsuiteItem.itemid);
                                    }


                                }

                            }
                            else
                            {
                                //if its exist

                                var PostMasTerData = new
                                {
                                    product = new
                                    {
                                        title = item_title,
                                        body_html = item_desc,
                                       
                                    }
                                };

                                var json_payload = JsonConvert.SerializeObject(PostMasTerData);

                                var check_addon_db = _context.PlatformItemTable.Where(x => x.Platform == "Shopify" && x.ReferenceId == netsuiteItem.product_id.ToString() && x.ItemId == netsuiteItem.id).FirstOrDefault();
                                if (!string.IsNullOrEmpty(item_title) && !string.IsNullOrEmpty(item_desc))
                                {
                                    if(json_payload!= check_addon_db.BuildJson)
                                    {
                                        var Parent_Posting = await PatchItemHeader(PostMasTerData, netsuiteItem.product_id);

                                    }

                                }

                                
                                if (check_addon_db != null)
                                {
                                    check_addon_db.BuildJson = JsonConvert.SerializeObject(PostMasTerData);
                                    check_addon_db.UploadDate = DateTime.Now;
                                    check_addon_db.Message = $"Master Item Updated {DateTime.Now}";
                                  
                                }
                                else
                                {
                                    if (string.IsNullOrEmpty(item_title) && string.IsNullOrEmpty(item_desc))
                                        continue;
                                        _context.PlatformItemTable.Add(new PlatformItemTable
                                        {
                                        ReferenceId = netsuiteItem.id.ToString(),
                                        Platform = "Shopify",
                                        ItemId = netsuiteItem.id.ToString(),
                                        BuildJson = JsonConvert.SerializeObject(PostMasTerData),
                                        UploadDate = DateTime.Now,
                                        Message = $"Master Item Processed {DateTime.Now}",
                                        //PlatformJson = Parent_Posting.Body.ToString()
                                    });

                                }
                                    _context.SaveChanges();

                                if (long.TryParse(netsuiteItem.product_id, out var parsedProductId))
                                {
                                    if (parsedProductId != 0)
                                    {
                                        await SyncHeaderImageToShopify(netsuiteItem.id, parsedProductId, netsuiteItem.itemid);
                                    }
                                }

                            }
                        }
                        else
                        {
                            //VARIANT
                            
                            string product_id = netsuiteItem.product_id?.ToString() ?? "";
                            string varian_id = netsuiteItem.variant_id?.ToString() ?? "";

                            if(!string.IsNullOrEmpty(product_id) && !string.IsNullOrEmpty(varian_id))
                            {
                                var checkaddonDB = _context.PlatformItemTable.Where(x => x.Platform == "Shopify" && x.ReferenceId == product_id && x.variantId == varian_id && x.ItemId == netsuiteItem.id).FirstOrDefault();
                                if (checkaddonDB != null)
                                {
                                  
                                    var payload = new
                                    {
                                        variant = new
                                        {
                                            id = long.Parse(netsuiteItem.product_id),
                                            option1 = netsuiteItem.displayname,
                                            price = netsuiteItem.sku_baseprice,
                                            //sku = netsuiteItem.itemid,
                                            inventory_management = "shopify"
                                        }
                                    };

                                    var serialize_json = System.Text.Json.JsonSerializer.Serialize(payload);

                                    var isExistToAddon = _context.PlatformItemTable.Where(x => x.ReferenceId == netsuiteItem.product_id && x.BuildJson == serialize_json && x.variantId==netsuiteItem.variant_id).Any();

                                    if (!isExistToAddon)
                                    {
                                        //Update Variant Existing
                                        var item_Post_result = await UpdateShopifyVariantAsync(netsuiteItem.variant_id, payload);


                                        if (item_Post_result.IsSuccess)
                                        {
                                            checkaddonDB.BuildJson = JsonConvert.SerializeObject(payload);
                                            checkaddonDB.UploadDate = DateTime.Now;
                                            checkaddonDB.Message = $"Master Item Updated {DateTime.Now}";
                                            checkaddonDB.PlatformJson = item_Post_result.Body.ToString();
                                        }
                                        else
                                        {
                                            _context.SystemLogs .Add(new DomainLayer.Models.SystemLog
                                            {
                                                Task = $"Error - Update Variant",
                                                ApiUrl = "UpdateShopifyVariantAsync",
                                                Json = $"VariantId: {netsuiteItem.variant_id}, Payload: {serialize_json}",
                                                ErrorMsg = $"Error {item_Post_result.StatusCode}: {item_Post_result.ReasonPhrase} - {item_Post_result.Body}",
                                                CreateDate = DateTime.Now,
                                            });
                                        }
                                            _context.SaveChanges();
                                       
                                    }

                                    
                                    //Function Posting of Image
                                    var PostImageToShopify = await CallPostImageToShopify(netsuiteItem, product_id,varian_id);


                                    //deletion of Variant
                                    var deletion_resul = await CallDeletionofVariant(product_id);
                                 

                                }
                                else
                                {
                                   

                                    //Its not existing on Database so need to check to Shopify if the variant is exist or not if exist we will update if not we will create
                                    var result = await shopifyHelper.CallShopifyGraphQLAsync(netsuiteItem.itemid);
                                    bool var_exist = shopifyHelper.ShopifyProductExists(result);
                                    if (var_exist)
                                    {
                                        //update variant then add it to database after
                                        var payload = new
                                        {
                                            variant = new
                                            {
                                                id = long.Parse(netsuiteItem.product_id),
                                                option1 = netsuiteItem.displayname,
                                                price = netsuiteItem.sku_baseprice,
                                                //sku = netsuiteItem.itemid,
                                                inventory_management = "shopify"
                                            }
                                        };

                                        var serialize_json = System.Text.Json.JsonSerializer.Serialize(payload);

                                        var isExistToAddon = _context.PlatformItemTable.Where(x => x.ReferenceId == netsuiteItem.product_id && x.BuildJson == serialize_json && x.variantId == netsuiteItem.variant_id).Any();

                                        if (!isExistToAddon)
                                        {
                                            //Update Variant Existing
                                            var item_Post_result = await UpdateShopifyVariantAsync(netsuiteItem.variant_id, payload);

                                            // if exist then update // if not add 
                                            if (item_Post_result.IsSuccess)
                                            {
                                                _context.PlatformItemTable.Add(new PlatformItemTable
                                                {
                                                    ReferenceId = netsuiteItem.product_id.ToString(),
                                                    variantId = netsuiteItem.variant_id.ToString(),
                                                    Platform = "Shopify",
                                                    ItemId = netsuiteItem.id.ToString(),
                                                    BuildJson = JsonConvert.SerializeObject(payload),
                                                    PlatformJson = item_Post_result.Body.ToString(),
                                                    UploadDate = DateTime.Now,
                                                    Message = $"Master Item Processed {DateTime.Now}"
                                                });
                                            }
                                            else
                                            {
                                                _context.SystemLogs.Add(new DomainLayer.Models.SystemLog
                                                {
                                                    Task = $"Line : 360 + Error - Update Variant",
                                                    ApiUrl = "UpdateShopifyVariantAsync",
                                                    Json = $"VariantId: {netsuiteItem.variant_id}, Payload: {serialize_json}",
                                                    ErrorMsg = $"Error {item_Post_result.StatusCode}: {item_Post_result.ReasonPhrase} - {item_Post_result.Body}",
                                                    CreateDate = DateTime.Now,
                                                });
                                            }
                                            _context.SaveChanges();
                                               
                                        }

                                        string skuCode = netsuiteItem.itemid.ToString();
                                   
                                        int totalquantityonhand = int.Parse(netsuiteItem.totalquantityonhand.ToString());
                                     
                                        
                                        bool is_success = await shopifyHelper.CallSetInventoryLevel(skuCode, netsuiteItem.totalquantityonhand.ToString());
                                        if(netsuiteItem.product_id ==null && netsuiteItem.variant_id == null)
                                        {

                                            await JsonCallScript(netsuiteItem.id, 348,long.Parse(netsuiteItem.product_id), netsuiteItem.variant_id.ToString(), netsuiteItem.sku_baseprice.ToString());
                                        }


                                        //Function Posting of Image
                                        var PostImageToShopify = await CallPostImageToShopify(netsuiteItem, product_id, varian_id);

                                        //deletion of Variant
                                        var deletion_resul = await CallDeletionofVariant(product_id);
                                        //Call Delete Shopify?
                                    }
                                    else
                                    {
                                        //Create Variant then add it to database after

                                        if (netsuiteItem.id.ToString() == null)
                                            throw new InvalidOperationException($"netsuiteItem is null'");

                                        if (!long.TryParse(netsuiteItem.product_id.ToString(), out var zproductId))
                                            throw new InvalidOperationException($"Invalid product_id: '{netsuiteItem.product_id}'");

                                        var payload = new
                                        {
                                            variant = new
                                            {
                                                id = zproductId,
                                                option1 = netsuiteItem.displayname,
                                                price = netsuiteItem.sku_baseprice,
                                                sku = netsuiteItem.itemid,
                                                inventory_management = "shopify"
                                            }
                                        };

                                        var serialize_json = System.Text.Json.JsonSerializer.Serialize(payload);
                                        var item_Post_result = await AddShopifyVariantAsync(netsuiteItem.product_id, payload);

                                        long productId = 0;
                                        long variantId = 0;
                                        long inventoryItemId = 0;

                                        using (var doc = JsonDocument.Parse(item_Post_result.Body.ToString()))
                                        {
                                            var root = doc.RootElement;

                                            if (!root.TryGetProperty("variant", out var variant))
                                            {
                                                _context.SystemLogs.Add(new DomainLayer.Models.SystemLog
                                                {
                                                    Task = $"Line : Code 500  - Update Variant",
                                                    ApiUrl = "UpdateShopifyVariantAsync",
                                                    Json = $"VariantId: {netsuiteItem.variant_id}, Payload: {serialize_json}",
                                                    ErrorMsg = $"Shopify response does not contain variant",
                                                    CreateDate = DateTime.Now,
                                                });
                                                _context.SaveChanges();
                                            }
                                                //throw new InvalidOperationException("Shopify response does not contain 'variant'");

                                            variantId = variant.GetProperty("id").GetInt64();
                                            productId = variant.GetProperty("product_id").GetInt64();
                                            inventoryItemId = variant.GetProperty("inventory_item_id").GetInt64();
                                        }

                                        if (item_Post_result.IsSuccess)
                                        {
                                            _context.PlatformItemTable.Add(new PlatformItemTable
                                            {
                                                ReferenceId = productId.ToString(),
                                                Platform = "Shopify",
                                                ItemId = netsuiteItem.id.ToString(),
                                                BuildJson = JsonConvert.SerializeObject(payload),
                                                PlatformJson = item_Post_result.Body.ToString(),
                                                UploadDate = DateTime.Now,
                                                Message = $"Master Item Processed {DateTime.Now}",
                                                CreatedDate = DateTime.Now

                                            });
                                        }
                                        else
                                        {
                                            _context.SystemLogs.Add(new DomainLayer.Models.SystemLog
                                            {
                                                Task = $"Line : 390 + Error - Update Variant",
                                                ApiUrl = "UpdateShopifyVariantAsync",
                                                Json = $"VariantId: {netsuiteItem.variant_id}, Payload: {serialize_json}",
                                                ErrorMsg = $"Error {item_Post_result.StatusCode}: {item_Post_result.ReasonPhrase} - {item_Post_result.Body}",
                                                CreateDate = DateTime.Now,
                                            });
                                        }
                                        _context.SaveChanges();

                                        if (!string.IsNullOrEmpty(productId.ToString()))
                                        {
                                            string skuCode = netsuiteItem.itemid.ToString();
                                      
                                             int totalquantityonhand = int.Parse(netsuiteItem.totalquantityonhand.ToString());

                                            bool is_success = await shopifyHelper.CallSetInventoryLevel(skuCode, netsuiteItem.totalquantityonhand.ToString());


                                            await JsonCallScript(netsuiteItem.id, 348, productId, variantId.ToString(), netsuiteItem.sku_baseprice.ToString());

                                            var PostImageToShopify = await CallPostImageToShopify(netsuiteItem, productId.ToString(), variantId.ToString());
                                        

                                            var deletion_resul = await CallDeletionofVariant(product_id);
                                          
                                        }
                                    }
                                }
                            }
                            else
                            {

                                //if its not exist 
                                //Create Variant
                                //Similar to the posting variant part in header creation but here we will loop for each variant and post it if the product id is exist but the variant id is not exist in shopify
                                
                                var cmd_get_children = $@"
                                    SELECT
                                        parent.custitem_dbti_title,
                                        parent.custitem_dbti_description,
                                        p.custrecord_dbti_ecom_platid AS product_id
                                    FROM item i
                                    JOIN item parent
                                        ON parent.id = i.parent
                                    JOIN customrecord_dbti_platform p
                                        ON p.custrecord_dbti_ecom_item = parent.id
                                    WHERE
                                        p.custrecord_dbti_ecom_customer = 348
                                        AND i.parent = {netsuiteItem.parent};
                                ";


                                var child_items = await nsHelper.GetNetsuiteJson(QueryEndpoint, cmd_get_children);

                                var firstValidProductId = child_items
                             .Where(x => x.product_id != null)
                             .Select(x => x.product_id)
                             .FirstOrDefault();

                            
                                if (firstValidProductId != null)
                                {
                                    //Just Add New Product Variant
                                    
                                    var payload = new
                                    {
                                        variant = new
                                        {
                                            id = firstValidProductId.ToString(),
                                            option1 = netsuiteItem.displayname,
                                            price = netsuiteItem.sku_baseprice,
                                            sku = netsuiteItem.itemid,
                                            inventory_management = "shopify"
                                        }
                                    };

                                    var serialize_json = System.Text.Json.JsonSerializer.Serialize(payload);
                                    var item_Post_result = await AddShopifyVariantAsync(firstValidProductId.ToString(), payload);

                                    long productId = 0;
                                    long variantId = 0;
                                    long inventoryItemId = 0;
                                    if (item_Post_result.IsSuccess)
                                    {
                                        using (var doc = JsonDocument.Parse(item_Post_result.Body.ToString()))
                                    {
                                        var root = doc.RootElement;

                                        if (!root.TryGetProperty("variant", out var variant))
                                            throw new InvalidOperationException("Shopify response does not contain 'variant'");

                                        variantId = variant.GetProperty("id").GetInt64();
                                        productId = variant.GetProperty("product_id").GetInt64();
                                        inventoryItemId = variant.GetProperty("inventory_item_id").GetInt64();
                                    }

                                    var update_exist = _context.PlatformItemTable.Where(x => x.ReferenceId == productId.ToString() && x.variantId == variantId.ToString() && x.BuildJson == serialize_json).FirstOrDefault();

                                  
                                        if (update_exist!=null)
                                        {
                                            update_exist.BuildJson = serialize_json;
                                            update_exist.PlatformJson = item_Post_result.Body.ToString();
                                            update_exist.UploadDate = DateTime.Now;
                                            update_exist.Message = $"Variant Patch Processed {DateTime.Now}";
                                            _context.SaveChanges();
                                        }
                                        else
                                        {
                                            _context.PlatformItemTable.Add(new PlatformItemTable
                                            {
                                                ReferenceId = productId.ToString(),
                                                variantId = variantId.ToString(),
                                                Platform = "Shopify",
                                                ItemId = netsuiteItem.id.ToString(),
                                                BuildJson = JsonConvert.SerializeObject(payload),
                                                PlatformJson = item_Post_result.Body.ToString(),
                                                UploadDate = DateTime.Now,
                                                Message = $"Variant Item Processed {DateTime.Now}",
                                                CreatedDate = DateTime.Now
                                                
                                            });

                                            _context.SaveChanges();
                                        }
                                            
                                    }
                                    else
                                    {
                                        _context.SystemLogs.Add(new DomainLayer.Models.SystemLog
                                        {
                                            Task = $"Line : 390 + Error - Update Variant",
                                            ApiUrl = "UpdateShopifyVariantAsync",
                                            Json = $"VariantId: {netsuiteItem.variant_id}, Payload: {serialize_json}",
                                            ErrorMsg = $"Error {item_Post_result.StatusCode}: {item_Post_result.ReasonPhrase} - {item_Post_result.Body}",
                                            CreateDate = DateTime.Now,
                                        });
                                        _context.SaveChanges();

                                    }


                                    if (!string.IsNullOrEmpty(productId.ToString()) && productId != 0)
                                    {
                                        string skuCode = netsuiteItem.itemid.ToString();
                               
                                        int totalquantityonhand = int.Parse(netsuiteItem.totalquantityonhand.ToString());

                                          bool is_success = await shopifyHelper.CallSetInventoryLevel(skuCode, netsuiteItem.totalquantityonhand.ToString());


                                         await JsonCallScript(netsuiteItem.id, 348, productId, variantId.ToString(), netsuiteItem.sku_baseprice.ToString());

                                       
                                        var PostImageToShopify = await CallPostImageToShopify(netsuiteItem, productId.ToString(), variantId.ToString());

                                        //Delete Variant Default Title
                                        var deletion_resul =await CallDeletionofVariant(productId.ToString());


                                    }

                                   
                                }
                              

                            }

                        }

                    }


                    string update_query = $@"IF EXISTS (SELECT 1 FROM dbo.netsuite_item_log where log_message = 'Shopify')
                                BEGIN
                                    UPDATE dbo.netsuite_item_log
                                    SET last_run_time    = current_run_time,
                                        current_run_time = '{dt}'
                                     where log_message = 'Shopify';
                                END
                                ELSE
                                BEGIN
                                    INSERT INTO dbo.netsuite_item_log (current_run_time, last_run_time,log_message)
                                    VALUES ('{dt}', '{lastCheck}','Shopify');
                                END";
                    DataAccess.Select(conz, update_query);


                }

                catch (Exception ex)
                {
                    throw new Exception("Error" + ex.Message);
                }
            }
           
   
        }

        public async Task<bool> CallDeletionofVariant(string productId)
        {
            try
            {
                var var_id_del = await GetShopifyProduct(productId.ToString());
                if (var_id_del.DefaultVariantId != null && var_id_del.DefaultProductId != null)
                {
                    var del_Body = await DeleteShopifyProductVariant(var_id_del.DefaultProductId, var_id_del.DefaultVariantId);

                    if (!del_Body.IsSuccess)
                    {
                        _context.SystemLogs.Add(new DomainLayer.Models.SystemLog
                        {
                            Task = $"Product Variant Deletion",
                            ApiUrl = "DeleteShopifyProductVariant",
                            Json = del_Body.Body.ToString(),
                            ErrorMsg = del_Body.ReasonPhrase.ToString(),
                            CreateDate = DateTime.Now,
                        });
                        _context.SaveChanges();
                    }
                }
            }
            catch(Exception ex)
            {
                return false;
            }

            return true;
        }

        public async Task<bool> SyncHeaderImageToShopify(string id,long _ProductID,string SKU)
        {
            try
            {
                //var get_jsonresult = await GetParentHeader_Images(677, "Shopify", "", FolderName);
                //var folderData = Newtonsoft.Json.JsonConvert.DeserializeObject<FolderData>(get_jsonresult.ToString());
                var QueryEndpoint = _context.APISetups.Where(x => x.APICode.Contains("NSQueryItemData")).Select(x => x.APIURL).FirstOrDefault();



                var check_picture = $@"
                            SELECT
                                custrecord_dbti_ecom_imagelink AS image_ids
                            FROM
                                customrecord_dbti_platform
                            WHERE
                                custrecord_dbti_ecom_item = {id}
                                AND custrecord_dbti_ecom_customer = 348
                        ";

                var internalIdsResult = await nsHelper.GetNetsuiteJson(QueryEndpoint, check_picture);

                var raw = internalIdsResult?
                            .FirstOrDefault()?
                            .image_ids?
                            .ToString();



                var dbIds = _context.FileCabinetImages
                       .Where(x => x.SKUFolder == SKU.ToString()
                           && !x.status.Contains("Deleted"))
                       .Select(x => x.FileID)   // <-- use your actual column
                       .ToList();


                var dbSet = dbIds
                        .Where(x => int.TryParse(x, out _))
                        .Select(int.Parse)
                        .ToHashSet();


                int[] internalIdArray = Array.Empty<int>();

                if (!string.IsNullOrWhiteSpace(raw))
                {
                    internalIdArray = Regex.Matches(raw, @"\d+")
                                           .Cast<Match>()
                                           .Select(m => int.Parse(m.Value))
                                           .ToArray();
                }


                var internalSet = internalIdArray.ToHashSet();

                //InternalId vs Databased Saved
                //compare if its existed on the database and not existed on the internal array



                // DO i need to loop all of the internalIdRayy? then check if its still aand

                var toUpload = internalSet.Except(dbSet).ToList();
                var toDelete = dbSet.Except(internalSet).ToList();


          
                if (toUpload.Count > 0)
                {
                    foreach (var upload in toUpload)
                    {
                        var imageDetails = await nsHelperMethods.GetImageDetails(upload.ToString());
                        var base64String = imageDetails?.base64 ?? "";
                        var imageName = imageDetails?.name ?? "";

                        var posting_result = await nsHelperMethods.PostImagetoShopify(_ProductID.ToString(), base64String, imageName);

                        if (!posting_result.Contains("error"))
                        {

                            _context.FileCabinetImages.Add(new FileCabinetImage
                            {
                                SKUFolder = SKU,
                                FileID = upload.ToString(),
                                Platform = "Shopify",
                                ImageID = _ProductID.ToString(),
                                ImageName = imageName,
                                Type = "Header",
                                ImageBinary = Convert.FromBase64String(base64String.ToString()),
                                CreatedDate = DateTime.Now,
                                status = posting_result.ToString(),
                            });

                            _context.SaveChanges();
                        }
                    }
                }



                if (toDelete.Count > 0)
                {
                    foreach (var delete in toDelete)
                    {
                        var Json_Image = _context.FileCabinetImages.Where(x => x.FileID == delete.ToString()).FirstOrDefault();

                        long imageId = 0;
                        using (JsonDocument doc = JsonDocument.Parse(Json_Image.status))
                        {
                            imageId = doc.RootElement
                                             .GetProperty("image")
                                             .GetProperty("id")
                                             .GetInt64();
                        }
                        var res = await nsHelperMethods.DeleteShopifyImage(_ProductID.ToString(), imageId.ToString());

                        if (res.IsSuccess)
                        {
                            Json_Image.status = $"Deleted - {imageId}";
                            Json_Image.UpdatedDate = DateTime.Now;
                            _context.SaveChanges();

                        }


                    }
                }


                return true;
            }
            catch (Exception ex)
            {
                _context.SystemLogs.Add(new DomainLayer.Models.SystemLog
                {
                    Task = $"Error - Update Master Item with ProductId Script Call",
                    ApiUrl = "SyncHeaderImageToShopify",
                    ErrorMsg = $"Error  - {DateTime.Now} - {ex.Message}",
                    CreateDate = DateTime.Now,
                });
                _context.SaveChanges();
                return false;
            }
        }





    
        public async Task<bool> CallPostImageToShopify(NetsuiteItemModel netsuiteItem,string product_id,string variant_id)
        {
            try
            {
                var QueryEndpoint = _context.APISetups.Where(x => x.APICode.Contains("NSQueryItemData")).Select(x => x.APIURL).FirstOrDefault();



                var check_picture = $@"
                            SELECT
                                custrecord_dbti_ecom_imagelink AS image_ids
                            FROM
                                customrecord_dbti_platform
                            WHERE
                                custrecord_dbti_ecom_item = {netsuiteItem.id}
                                AND custrecord_dbti_ecom_customer = 348
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
                }


                var firstId = internalIdArray.FirstOrDefault();

                if (firstId == 0)
                    return true;

                var old_FileId = _context.FileCabinetImages.Where(x => x.SKUFolder == netsuiteItem.itemid && x.FileID != null && x.Platform == "Shopify").FirstOrDefault();
                
                if (old_FileId?.FileID != firstId.ToString())
                {
                    var check_image_db = _context.FileCabinetImages.Where(x => x.SKUFolder == netsuiteItem.itemid && x.FileID == firstId.ToString() && x.Platform == "Shopify").FirstOrDefault();

                    var imageDetails = await GetImageDetails(firstId.ToString());
                    var base64String = imageDetails?.base64 ?? "";
                    var imageName = imageDetails?.name ?? "";

                    var posting_result = await PostImagetoShopify(product_id, base64String, imageName, variant_id);

                    if (!posting_result.Contains("error"))
                    {

                        if (check_image_db == null && old_FileId == null)
                        {
                            //Add

                            _context.FileCabinetImages.Add(new FileCabinetImage
                            {
                                SKUFolder = netsuiteItem.itemid,
                                FileID = firstId.ToString(),
                                Platform = "Shopify",
                                ImageID = variant_id,
                                ImageName = imageName,
                                Type = "Variant",
                                ImageBinary = Convert.FromBase64String(base64String.ToString()),
                                CreatedDate = DateTime.Now,
                                status = posting_result.ToString(),


                            });

                            _context.SaveChanges();
                        }
                        else
                        {

                            long imageId = 0;
                            using (JsonDocument doc = JsonDocument.Parse(old_FileId.status))
                            {
                                 imageId = doc.RootElement
                                                  .GetProperty("image")
                                                  .GetProperty("id")
                                                  .GetInt64();
                            }
                            old_FileId.FileID = firstId.ToString();
                            old_FileId.Platform = "Shopify";
                            old_FileId.ImageID = variant_id.ToString();

                            old_FileId.ImageBinary = Convert.FromBase64String(base64String.ToString());
                            old_FileId.ImageName = imageName;
                            old_FileId.UpdatedDate = DateTime.Now;
                            old_FileId.status = posting_result.ToString();

                            _context.SaveChanges();

                            await DeleteShopifyImage(product_id.ToString(), imageId.ToString());

                        }
                    }



                }
            }
            catch(Exception ex)
            {

                _context.SystemLogs.Add(new DomainLayer.Models.SystemLog
                {
                    Task = "CallPostImageToShopify",
                    CreateDate = DateTime.Now,
                    ApiUrl = "api/shopify/syncImage",
                    ErrorMsg = ex.Message,
                    Module = "Shopify Products"
                });
                return false;
            }
            return true;
        }
        static string FixJson(string json)
        {
            return Regex.Replace(json, @",\s*([\]}])", "$1");
        }
        public async Task<HttpResponseMessage> PostShopifyProduct(string JsonBody)
            {
                var APISetup = _context.APISetups.FirstOrDefault(x => x.APICode == "ShopifyAPI");
                if (APISetup == null)
                    throw new Exception("Invalid API configuration.");


                string accessToken = APISetup.APIToken;

                var url = $"https://strokes-beauty.myshopify.com/admin/api/2025-10/products.json";

                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("X-Shopify-Access-Token", accessToken);
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    var content = new StringContent(JsonBody, Encoding.UTF8, "application/json");

                    var response = await client.PostAsync(url, content);

               
                    if (!response.IsSuccessStatusCode)
                        throw new Exception($"Shopify error {response.StatusCode}: {response.ReasonPhrase}");

                    return response;
                  
                }
                
            }
        public async Task<HttpResponseMessage> PostShopifyProductVariant(string JsonBody,string ProductId)
        {
            var APISetup = _context.APISetups.FirstOrDefault(x => x.APICode == "ShopifyAPI");
            if (APISetup == null)
                throw new Exception("Invalid API configuration.");


            string accessToken = APISetup.APIToken;

            var url = $"https://strokes-beauty.myshopify.com/admin/api/2025-10/products/{ProductId}.json";

            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("X-Shopify-Access-Token", accessToken);
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                var content = new StringContent(JsonBody, Encoding.UTF8, "application/json");

                var response = await client.PostAsync(url, content);


                if (!response.IsSuccessStatusCode)
                    throw new Exception($"Shopify error {response.StatusCode}: {response.ReasonPhrase}");

                return response;
                //var json = await response.Content.ReadAsStringAsync();
                //return JsonConvert.DeserializeObject<ShopifyProductModel>(json);
            }

        }
        public async Task<ShopifyApiResult> UpdateShopifyVariantAsync(string variantId, object payload)
        {
            var APISetup = _context.APISetups.FirstOrDefault(x => x.APICode == "ShopifyAPI");
            if (APISetup == null)
                throw new Exception("Invalid API configuration.");

            string accessToken = APISetup.APIToken;

            // ✅ Correct REST endpoint for updating a variant
            string url = $"https://strokes-beauty.myshopify.com/admin/api/2025-10/variants/{variantId}.json";

            var jsonBody = System.Text.Json.JsonSerializer.Serialize(payload);

            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("X-Shopify-Access-Token", accessToken);
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                using (var content = new StringContent(jsonBody, Encoding.UTF8, "application/json"))
                {
                    // ✅ Use PUT (Shopify REST)
                    var response = await client.PutAsync(url, content);
                    var body = await response.Content.ReadAsStringAsync();

                    return new ShopifyApiResult
                    {
                        StatusCode = (int)response.StatusCode,
                        IsSuccess = response.IsSuccessStatusCode,
                        ReasonPhrase = response.ReasonPhrase,
                        Body = body
                    };
                }
            }
        }
        public async Task<ShopifyApiResult> AddShopifyVariantAsync(string productId, object payload)
        {
            var APISetup = _context.APISetups.FirstOrDefault(x => x.APICode == "ShopifyAPI");
            if (APISetup == null)
                throw new Exception("Invalid API configuration.");

            string accessToken = APISetup.APIToken;

            // ✅ Create variant under a product
            string url = $"https://strokes-beauty.myshopify.com/admin/api/2025-10/products/{productId}/variants.json";

            // IMPORTANT: payload must be { variant = { ... } }
            var jsonBody = System.Text.Json.JsonSerializer.Serialize(payload);

            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("X-Shopify-Access-Token", accessToken);
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                using (var content = new StringContent(jsonBody, Encoding.UTF8, "application/json"))
                {
                    // ✅ Use POST to create
                    var response = await client.PostAsync(url, content);
                    var body = await response.Content.ReadAsStringAsync();

                    return new ShopifyApiResult
                    {
                        StatusCode = (int)response.StatusCode,
                        IsSuccess = response.IsSuccessStatusCode,
                        ReasonPhrase = response.ReasonPhrase,
                        Body = body
                    };
                }
            }
        }


        public async Task<string> SetInventoryLevelAsync(string inventoryItemId, int available)
        {
            var APISetup = _context.APISetups.FirstOrDefault(x => x.APICode == "ShopifyAPI");
            if (APISetup == null)
                throw new Exception("Invalid API configuration.");

            string accessToken = APISetup.APIToken;

            var url = "https://strokes-beauty.myshopify.com/admin/api/2025-01/inventory_levels/set.json";

            var payload = new
            {
                location_id = 52564852903L,
                inventory_item_id = inventoryItemId,
                available = available
            };
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("X-Shopify-Access-Token", accessToken);
                client.DefaultRequestHeaders.Accept.Add(
                    new MediaTypeWithQualityHeaderValue("application/json")
                );

                var json = System.Text.Json.JsonSerializer.Serialize(payload);

                using (var content = new StringContent(
                    json,
                    Encoding.UTF8,
                    "application/json"
                ))
                {
                    var response = await client.PostAsync(url, content);
                    var responseBody = await response.Content.ReadAsStringAsync();
                    if (!response.IsSuccessStatusCode)
                    {
                        var errorBody = await response.Content.ReadAsStringAsync();

                        return $"Shopify error {response.StatusCode}: {errorBody}";
                      
                    }
                    return responseBody;
                
                }


               
            }

            //return true;
        }

        public async Task<string> GetParentHeader_Images(
            int script,
            string platform,
            string Code,
            string FolderName = null
            )
        {

            //var sched = _context.Schedules.Where(sel => sel.SchedCode == Code).FirstOrDefault();
            try
            {
                // ===== NetSuite Credentials =====
                string accountId = ConfigurationManager.AppSettings["accountId"];
                string consumerKey = ConfigurationManager.AppSettings["consumerKey"];
                string consumerSecret = ConfigurationManager.AppSettings["consumerSecret"];
                string tokenId = ConfigurationManager.AppSettings["tokenId"];
                string tokenSecret = ConfigurationManager.AppSettings["tokenSecret"];

                // ===== Base URL (NO query params here) =====
                string baseUrl = $"https://{accountId}.restlets.api.netsuite.com/app/site/hosting/restlet.nl";

                // ===== Query Parameters (MUST be signed) =====
                // -------------------------------
                // Query parameters (SIGNED)
                // -------------------------------
                Dictionary<string, string> queryParams = new Dictionary<string, string>();
                queryParams.Add("script", script.ToString());
                queryParams.Add("deploy", "1");
                queryParams.Add("parentFolderName", platform);
                if (FolderName != null)
                {
                    queryParams.Add("childFolderName", FolderName);
                    
                }
                //queryParams.Add("sinceDate", sinceDate);

                // -------------------------------
                // OAuth parameters
                // -------------------------------
                Dictionary<string, string> oauthParams = new Dictionary<string, string>();
                oauthParams.Add("oauth_consumer_key", consumerKey);
                oauthParams.Add("oauth_token", tokenId);
                oauthParams.Add("oauth_nonce", Guid.NewGuid().ToString("N"));
                oauthParams.Add("oauth_timestamp",
                    ((int)(DateTime.UtcNow -
                    new DateTime(1970, 1, 1)).TotalSeconds).ToString());
                oauthParams.Add("oauth_signature_method", "HMAC-SHA256");
                oauthParams.Add("oauth_version", "1.0");

                // -------------------------------
                // Combine params for signing
                // -------------------------------
                Dictionary<string, string> allParams = new Dictionary<string, string>();
                foreach (var kv in queryParams) allParams.Add(kv.Key, kv.Value);
                foreach (var kv in oauthParams) allParams.Add(kv.Key, kv.Value);

                // -------------------------------
                // Normalize parameters
                // -------------------------------
                List<string> paramList = new List<string>();
                foreach (var kv in allParams)
                {
                    paramList.Add(
                        UrlEncode(kv.Key) + "=" + UrlEncode(kv.Value)
                    );
                }
                paramList.Sort();
                string normalizedParams = string.Join("&", paramList.ToArray());

                // -------------------------------
                // Signature base string
                // -------------------------------
                string signatureBase =
                    "GET&" +
                    UrlEncode(baseUrl) + "&" +
                    UrlEncode(normalizedParams);

                string signingKey =
                    UrlEncode(consumerSecret) + "&" +
                    UrlEncode(tokenSecret);

                string signature;
                using (HMACSHA256 hmac =
                    new HMACSHA256(Encoding.ASCII.GetBytes(signingKey)))
                {
                    signature = Convert.ToBase64String(
                        hmac.ComputeHash(Encoding.ASCII.GetBytes(signatureBase))
                    );
                }

                // -------------------------------
                // Authorization header
                // -------------------------------
                StringBuilder authHeader = new StringBuilder();
                authHeader.Append("OAuth realm=\"");
                authHeader.Append(accountId);
                authHeader.Append("\",");

                foreach (var kv in oauthParams)
                {
                    authHeader.Append(kv.Key);
                    authHeader.Append("=\"");
                    authHeader.Append(UrlEncode(kv.Value));
                    authHeader.Append("\",");
                }

                authHeader.Append("oauth_signature=\"");
                authHeader.Append(UrlEncode(signature));
                authHeader.Append("\"");

                // -------------------------------
                // Request URL
                // -------------------------------
                StringBuilder requestUrl = new StringBuilder();
                requestUrl.Append(baseUrl);
                requestUrl.Append("?");

                bool first = true;
                foreach (var kv in queryParams)
                {
                    if (!first) requestUrl.Append("&");
                    first = false;
                    requestUrl.Append(kv.Key);
                    requestUrl.Append("=");
                    requestUrl.Append(UrlEncode(kv.Value));
                }

                // -------------------------------
                // HTTP request
                // -------------------------------
                using (HttpClient client = new HttpClient())
                using (HttpRequestMessage request =
                    new HttpRequestMessage(HttpMethod.Get, requestUrl.ToString()))
                {
                    request.Headers.Add("Authorization", authHeader.ToString());
                    request.Headers.Add("Accept", "application/json");

                    HttpResponseMessage response = await client.SendAsync(request);
                    string body = await response.Content.ReadAsStringAsync();

                    if (!response.IsSuccessStatusCode)
                        throw new Exception(body);


                    
                    return body;
                }

                
            }
            catch(Exception ex)
            {
                throw new Exception(ex.Message);
            }
            
        }



        /// Posting Images To Netsuite after Getting Base64
        public async Task<string> PostShopifyImage(string code)
        {
            try
            {
                var CabinetImages = _context.FileCabinetImages.Where(x => x.Platform == "Shopify" && x.ImageID==null && x.ImageBinary == null).ToList();

                foreach (var cabinet_img in CabinetImages)
                {
                    var FileID = cabinet_img.FileID;
                    var SKuCode = cabinet_img.SKUFolder;
                    //dynamic ImageDetails =
                    var imageDetails = await GetImageDetails(FileID);
                    var base64String = imageDetails.base64;
                    var imageName = imageDetails.name;
                    if(imageDetails.success && base64String!=null && imageName != null)
                    {
                        var result = await shopifyHelper.CallShopifyGraphQLAsync(SKuCode);

                        string productId =
                            ((string)JObject.Parse(result)["data"]?["productVariants"]?["edges"]?[0]?["node"]?["product"]?["id"])
                            ?.Split('/')
                            .Last();

                        string VariantId =
                            ((string)JObject.Parse(result)["data"]?["productVariants"]?["edges"]?[0]?["node"]?["id"])
                            ?.Split('/')
                            .Last();
                        if (!string.IsNullOrEmpty(productId))
                        {
                            var posting_result = await PostImagetoShopify(productId, base64String,imageName, VariantId);

                            if (!posting_result.Contains("error"))
                            {
                                cabinet_img.ImageID = VariantId;
                                cabinet_img.Platform="Shopify";
                                // or DateTime.Now
                                cabinet_img.ImageBinary = Convert.FromBase64String(base64String.ToString());
                                cabinet_img.status = posting_result.ToString();// or a flag like "Uploaded"
                                cabinet_img.UpdatedDate = DateTime.Now;
                            }
                            else
                            {
                                _context.SystemLogs.Add(new DomainLayer.Models.SystemLog
                                {
                                    Task = "PostImagetoShopify",
                                    CreateDate = DateTime.Now,
                                    ApiUrl = "api/shopify/syncImage",
                                    ErrorMsg = posting_result,
                                    Module = "Shopify Products"
                                });
                              
                            }
                            _context.SaveChanges();

                        }
                    }

                }
                return "";
            }
            catch(Exception ex)
            {
                throw new Exception(ex.Message);
            }   
          
        }

        public async Task<dynamic> GetImageDetails(string FileId)
        {
            // ===== NetSuite Credentials =====
            string accountId = ConfigurationManager.AppSettings["accountId"];
            string consumerKey = ConfigurationManager.AppSettings["consumerKey"];
            string consumerSecret = ConfigurationManager.AppSettings["consumerSecret"];
            string tokenId = ConfigurationManager.AppSettings["tokenId"];
            string tokenSecret = ConfigurationManager.AppSettings["tokenSecret"];

            // ===== Base URL (NO query params here) =====
            string baseUrl = $"https://{accountId}.restlets.api.netsuite.com/app/site/hosting/restlet.nl";

            // ===== Query Parameters (MUST be signed) =====
            // -------------------------------
            // Query parameters (SIGNED)
            // -------------------------------
            Dictionary<string, string> queryParams = new Dictionary<string, string>();
            queryParams.Add("script", "671");
            queryParams.Add("deploy", "1");
            queryParams.Add("fileId", FileId);
           

            // -------------------------------
            // OAuth parameters
            // -------------------------------
            Dictionary<string, string> oauthParams = new Dictionary<string, string>();
            oauthParams.Add("oauth_consumer_key", consumerKey);
            oauthParams.Add("oauth_token", tokenId);
            oauthParams.Add("oauth_nonce", Guid.NewGuid().ToString("N"));
            oauthParams.Add("oauth_timestamp",
                ((int)(DateTime.UtcNow -
                new DateTime(1970, 1, 1)).TotalSeconds).ToString());
            oauthParams.Add("oauth_signature_method", "HMAC-SHA256");
            oauthParams.Add("oauth_version", "1.0");

            // -------------------------------
            // Combine params for signing
            // -------------------------------
            Dictionary<string, string> allParams = new Dictionary<string, string>();
            foreach (var kv in queryParams) allParams.Add(kv.Key, kv.Value);
            foreach (var kv in oauthParams) allParams.Add(kv.Key, kv.Value);

            // -------------------------------
            // Normalize parameters
            // -------------------------------
            List<string> paramList = new List<string>();
            foreach (var kv in allParams)
            {
                paramList.Add(
                    UrlEncode(kv.Key) + "=" + UrlEncode(kv.Value)
                );
            }
            paramList.Sort();
            string normalizedParams = string.Join("&", paramList.ToArray());

            // -------------------------------
            // Signature base string
            // -------------------------------
            string signatureBase =
                "GET&" +
                UrlEncode(baseUrl) + "&" +
                UrlEncode(normalizedParams);

            string signingKey =
                UrlEncode(consumerSecret) + "&" +
                UrlEncode(tokenSecret);

            string signature;
            using (HMACSHA256 hmac =
                new HMACSHA256(Encoding.ASCII.GetBytes(signingKey)))
            {
                signature = Convert.ToBase64String(
                    hmac.ComputeHash(Encoding.ASCII.GetBytes(signatureBase))
                );
            }

            // -------------------------------
            // Authorization header
            // -------------------------------
            StringBuilder authHeader = new StringBuilder();
            authHeader.Append("OAuth realm=\"");
            authHeader.Append(accountId);
            authHeader.Append("\",");

            foreach (var kv in oauthParams)
            {
                authHeader.Append(kv.Key);
                authHeader.Append("=\"");
                authHeader.Append(UrlEncode(kv.Value));
                authHeader.Append("\",");
            }

            authHeader.Append("oauth_signature=\"");
            authHeader.Append(UrlEncode(signature));
            authHeader.Append("\"");

            // -------------------------------
            // Request URL
            // -------------------------------
            StringBuilder requestUrl = new StringBuilder();
            requestUrl.Append(baseUrl);
            requestUrl.Append("?");

            bool first = true;
            foreach (var kv in queryParams)
            {
                if (!first) requestUrl.Append("&");
                first = false;
                requestUrl.Append(kv.Key);
                requestUrl.Append("=");
                requestUrl.Append(UrlEncode(kv.Value));
            }

            using (HttpClient client = new HttpClient())
            using (HttpRequestMessage request =
                new HttpRequestMessage(HttpMethod.Get, requestUrl.ToString()))
            {
                request.Headers.Add("Authorization", authHeader.ToString());
                request.Headers.Add("Accept", "application/json");

                HttpResponseMessage response = await client.SendAsync(request);
                string body = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                    throw new Exception(body);


                var obj = JsonConvert.DeserializeObject<ImageDetailsResponse>(body);

                if (obj == null || obj.success != true)
                    return "";

                return obj;
            }

              
        }

        public async Task<string> PostImagetoShopify(string ProductId,string Base64,string ImageName,string VariantId)
        {
            try
            {
                var APISetup = _context.APISetups.FirstOrDefault(x => x.APICode == "ShopifyAPI");
                if (APISetup == null)
                    throw new Exception("Invalid API configuration.");

                string accessToken = APISetup.APIToken;

                var url = $"https://strokes-beauty.myshopify.com/admin/api/2025-01/products/{ProductId}/images.json";

                var payload = new
                {
                    image = new
                    {
                        attachment = Base64,
                        alt = ImageName,
                        variant_ids = new long[] { long.Parse(VariantId) }
                    }
                };
                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("X-Shopify-Access-Token", accessToken);
                    client.DefaultRequestHeaders.Accept.Add(
                        new MediaTypeWithQualityHeaderValue("application/json")
                    );

                    var json = System.Text.Json.JsonSerializer.Serialize(payload);

                    using (var content = new StringContent(
                        json,
                        Encoding.UTF8,
                        "application/json"
                    ))
                    {
                        var response = await client.PostAsync(url, content);
                        var responseBody = await response.Content.ReadAsStringAsync();
                        if (!response.IsSuccessStatusCode)
                        {
                            var errorBody = await response.Content.ReadAsStringAsync();

                            return $"Shopify error {response.StatusCode}: {errorBody}";

                        }
                        return responseBody;

                    }

                }
            }
            catch(Exception ex)
            {
                return $"error - { ex.Message.ToString()}";
            }
           
          
        }
    public async Task<ShopifyApiResult> DeleteShopifyImage(
    string productId,
    string imageId)
        {
            try
            {
                var APISetup = _context.APISetups.FirstOrDefault(x => x.APICode == "ShopifyAPI");
                if (APISetup == null)
                    throw new Exception("Invalid API configuration.");

                string accessToken = APISetup.APIToken;

                var url = $"https://strokes-beauty.myshopify.com/admin/api/2025-01/products/{productId}/images/{imageId}.json";


                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("X-Shopify-Access-Token", accessToken);
                    client.DefaultRequestHeaders.Accept.Add(
                        new MediaTypeWithQualityHeaderValue("application/json")
                    );

                    var response = await client.DeleteAsync(url);
                    var responseBody = await response.Content.ReadAsStringAsync();

                    if (response.StatusCode == HttpStatusCode.NotFound)
                    {
                        return new ShopifyApiResult
                        {
                            StatusCode = 404,
                            IsSuccess = true,
                            ReasonPhrase = "Image already deleted",
                            Body = responseBody
                        };
                    }

                    return new ShopifyApiResult
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

        public async Task<ShopifyApiResult> PostItemHeaderToShopify(object payloadJson)
        {
            try
            {
                var APISetup = _context.APISetups.FirstOrDefault(x => x.APICode == "ShopifyAPI");
                if (APISetup == null)
                    throw new Exception("Invalid API configuration.");

                string accessToken = APISetup.APIToken;
                var url = "https://strokes-beauty.myshopify.com/admin/api/2025-01/products.json";

                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("X-Shopify-Access-Token", accessToken);
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    var json = System.Text.Json.JsonSerializer.Serialize(payloadJson);

                    using (var content = new StringContent(json, Encoding.UTF8, "application/json"))
                    {
                        var response = await client.PostAsync(url, content);
                        var body = await response.Content.ReadAsStringAsync(); // ✅ read once

                        //if (!response.IsSuccessStatusCode)
                        //    return $"Shopify error {(int)response.StatusCode} {response.ReasonPhrase}: {body}";

                        return new ShopifyApiResult
                        {
                            StatusCode = (int)response.StatusCode,
                            IsSuccess = response.IsSuccessStatusCode,
                            ReasonPhrase = response.ReasonPhrase,
                            Body = body
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"error - {ex.Message}");
            }

        }

        public async Task<ShopifyApiResult> GetShopifyProduct(string product_id)
        {
            try
            {
                var APISetup = _context.APISetups.FirstOrDefault(x => x.APICode == "ShopifyAPI");
                if (APISetup == null)
                    throw new Exception("Invalid API configuration.");

                string accessToken = APISetup.APIToken;
                var url = $"https://strokes-beauty.myshopify.com/admin/api/2025-01/products/{product_id}.json"; // Corrected the URL format

                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("X-Shopify-Access-Token", accessToken);
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    // GET request instead of POST
                    var response = await client.GetAsync(url); // Use GetAsync instead of PostAsync
                    var body = await response.Content.ReadAsStringAsync(); // ✅ read once

                    // Deserialize JSON into a dynamic object
                    var productData = JsonConvert.DeserializeObject<dynamic>(body);

                    // Loop through the variants to find the "Default Title"
                    dynamic defaultVariant = null;
                    foreach (var variant in productData.product.variants)
                    {
                        if ((string)variant.title == "Default Title")
                        {
                            defaultVariant = variant;
                            break;
                        }
                    }

                    if (defaultVariant != null)
                    {
                        // Return the variant ID if found
                        return new ShopifyApiResult
                        {
                            StatusCode = (int)response.StatusCode,
                            IsSuccess = response.IsSuccessStatusCode,
                            ReasonPhrase = response.ReasonPhrase,
                            Body = body,
                            DefaultVariantId = defaultVariant.id.ToString(),
                            DefaultProductId = defaultVariant.product_id.ToString() // Returning the Default Variant ID

                        };
                    }
                    else
                    {
                        // Handle case where "Default Title" variant is not found
                        return new ShopifyApiResult
                        {
                            StatusCode = (int)response.StatusCode,
                            IsSuccess = response.IsSuccessStatusCode,
                            ReasonPhrase = response.ReasonPhrase,
                            Body = body,
                            DefaultVariantId = null,
                            DefaultProductId = null // No variant found
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error - {ex.Message}");
            }
        }
        public async Task<ShopifyApiResult> DeleteShopifyProductVariant(string product_id, string variant_id)
        {
            try
            {
                var APISetup = _context.APISetups.FirstOrDefault(x => x.APICode == "ShopifyAPI");
                if (APISetup == null)
                    throw new Exception("Invalid API configuration.");

                string accessToken = APISetup.APIToken;
                var url = $"https://strokes-beauty.myshopify.com/admin/api/2025-01/products/{product_id}/variants/{variant_id}.json"; // Corrected the URL format for deleting a variant

                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("X-Shopify-Access-Token", accessToken);
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    // DELETE request to remove the variant
                    var response = await client.DeleteAsync(url); // Use DeleteAsync to delete the variant
                    var body = await response.Content.ReadAsStringAsync(); // ✅ read once

                    return new ShopifyApiResult
                    {
                        StatusCode = (int)response.StatusCode,
                        IsSuccess = response.IsSuccessStatusCode,
                        ReasonPhrase = response.ReasonPhrase,
                        Body = body
                    };
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error - {ex.Message}");
            }
        }



        public async Task<ShopifyApiResult> PatchItemHeader(object payloadJson,string ProductId)
        {
            try
            {
                var APISetup = _context.APISetups.FirstOrDefault(x => x.APICode == "ShopifyAPI");
                if (APISetup == null)
                    throw new Exception("Invalid API configuration.");

                string accessToken = APISetup.APIToken;
                var url = $"https://strokes-beauty.myshopify.com/admin/api/2025-01/products/{ProductId}.json";

                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("X-Shopify-Access-Token", accessToken);
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    var json = System.Text.Json.JsonSerializer.Serialize(payloadJson);

                    using (var content = new StringContent(json, Encoding.UTF8, "application/json"))
                    {
                        var response = await client.PutAsync(url, content);
                        var body = await response.Content.ReadAsStringAsync(); // ✅ read once

                        //if (!response.IsSuccessStatusCode)
                        //    return $"Shopify error {(int)response.StatusCode} {response.ReasonPhrase}: {body}";

                        return new ShopifyApiResult
                        {
                            StatusCode = (int)response.StatusCode,
                            IsSuccess = response.IsSuccessStatusCode,
                            ReasonPhrase = response.ReasonPhrase,
                            Body = body
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"error - {ex.Message}");
            }

        }

     public async Task<ShopifyApiResult> JsonCallScript(
     string itemId,
     int customerId,
     long productId,
     string variantId = null,
     string basePrice = null
 )
        {
            try
            {
                // ===== NetSuite Credentials =====
                string accountId = ConfigurationManager.AppSettings["accountId"];
                string consumerKey = ConfigurationManager.AppSettings["consumerKey"];
                string consumerSecret = ConfigurationManager.AppSettings["consumerSecret"];
                string tokenId = ConfigurationManager.AppSettings["tokenId"];
                string tokenSecret = ConfigurationManager.AppSettings["tokenSecret"];

                // ===== Base URL (NO query params here) =====
                string baseUrl = $"https://{accountId}.restlets.api.netsuite.com/app/site/hosting/restlet.nl";

                // -------------------------------
                // Query parameters (SIGNED)
                // -------------------------------
                Dictionary<string, string> queryParams = new Dictionary<string, string>
        {
            { "script", "674" },
            { "deploy", "1" },
            { "ecomId", customerId.ToString() },
            { "productId", productId.ToString() },
            { "itemId", itemId }
        };

                if (!string.IsNullOrWhiteSpace(variantId))
                    queryParams.Add("variantId", variantId);

                if (!string.IsNullOrWhiteSpace(basePrice))
                    queryParams.Add("basePrice", basePrice);

                // -------------------------------
                // OAuth parameters
                // -------------------------------
                Dictionary<string, string> oauthParams = new Dictionary<string, string>
        {
            { "oauth_consumer_key", consumerKey },
            { "oauth_token", tokenId },
            { "oauth_nonce", Guid.NewGuid().ToString("N") },
            {
                "oauth_timestamp",
                ((int)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds).ToString()
            },
            { "oauth_signature_method", "HMAC-SHA256" },
            { "oauth_version", "1.0" }
        };

                // -------------------------------
                // Combine ALL params for signing
                // -------------------------------
                Dictionary<string, string> allParams = new Dictionary<string, string>();
                foreach (var kv in queryParams) allParams.Add(kv.Key, kv.Value);
                foreach (var kv in oauthParams) allParams.Add(kv.Key, kv.Value);

                // -------------------------------
                // Normalize parameters
                // -------------------------------
                List<string> paramList = new List<string>();
                foreach (var kv in allParams)
                {
                    paramList.Add(
                        UrlEncode(kv.Key) + "=" + UrlEncode(kv.Value)
                    );
                }
                paramList.Sort();
                string normalizedParams = string.Join("&", paramList);

                // -------------------------------
                // Signature base string
                // -------------------------------
                string signatureBase =
                    "GET&" +
                    UrlEncode(baseUrl) + "&" +
                    UrlEncode(normalizedParams);

                string signingKey =
                    UrlEncode(consumerSecret) + "&" +
                    UrlEncode(tokenSecret);

                string signature;
                using (HMACSHA256 hmac =
                    new HMACSHA256(Encoding.ASCII.GetBytes(signingKey)))
                {
                    signature = Convert.ToBase64String(
                        hmac.ComputeHash(Encoding.ASCII.GetBytes(signatureBase))
                    );
                }

                // -------------------------------
                // Authorization header
                // -------------------------------
                StringBuilder authHeader = new StringBuilder();
                authHeader.Append("OAuth realm=\"");
                authHeader.Append(accountId);
                authHeader.Append("\",");

                foreach (var kv in oauthParams)
                {
                    authHeader.Append(kv.Key);
                    authHeader.Append("=\"");
                    authHeader.Append(UrlEncode(kv.Value));
                    authHeader.Append("\",");
                }

                authHeader.Append("oauth_signature=\"");
                authHeader.Append(UrlEncode(signature));
                authHeader.Append("\"");

                // -------------------------------
                // Request URL
                // -------------------------------
                StringBuilder requestUrl = new StringBuilder();
                requestUrl.Append(baseUrl);
                requestUrl.Append("?");

                bool first = true;
                foreach (var kv in queryParams)
                {
                    if (!first) requestUrl.Append("&");
                    first = false;
                    requestUrl.Append(kv.Key);
                    requestUrl.Append("=");
                    requestUrl.Append(UrlEncode(kv.Value));
                }

                // -------------------------------
                // HTTP request
                // -------------------------------
                using (HttpClient client = new HttpClient())
                using (HttpRequestMessage request =
                    new HttpRequestMessage(HttpMethod.Get, requestUrl.ToString()))
                {
                    request.Headers.Add("Authorization", authHeader.ToString());
                    request.Headers.Add("Accept", "application/json");

                    HttpResponseMessage response = await client.SendAsync(request);
                    string body = await response.Content.ReadAsStringAsync();

                    return new ShopifyApiResult
                    {
                        IsSuccess = response.IsSuccessStatusCode,
                        Body = body,
                        ReasonPhrase = response.ReasonPhrase
                    };
                }
            }
            catch (Exception ex)
            {
                return new ShopifyApiResult
                {
                    IsSuccess = false,
                    Body = ex.Message
                };
            }
        }




        public async Task<ShopifyApiResult> PatchHeader(object payloadJson,string ProductId)
        {
            try
            {
                var APISetup = _context.APISetups.FirstOrDefault(x => x.APICode == "ShopifyAPI");
                if (APISetup == null)
                    throw new Exception("Invalid API configuration.");

                string accessToken = APISetup.APIToken;
                var url = $"https://strokes-beauty.myshopify.com/admin/api/2025-01/products/{ProductId}.json";

                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("X-Shopify-Access-Token", accessToken);
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    var json = System.Text.Json.JsonSerializer.Serialize(payloadJson);

                    using (var content = new StringContent(json, Encoding.UTF8, "application/json"))
                    {
                        var response = await client.PutAsync(url, content);
                        var body = await response.Content.ReadAsStringAsync(); // ✅ read once

                        //if (!response.IsSuccessStatusCode)
                        //    return $"Shopify error {(int)response.StatusCode} {response.ReasonPhrase}: {body}";

                        return new ShopifyApiResult
                        {
                            StatusCode = (int)response.StatusCode,
                            IsSuccess = response.IsSuccessStatusCode,
                            ReasonPhrase = response.ReasonPhrase,
                            Body = body
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"error - {ex.Message}");
            }


        }

    public async Task<string> GetSingleSKUImage(
      int script,
      string platform,
      string SKUFolder)
        {
            try
            {
                // ===== NetSuite Credentials =====
                string accountId = ConfigurationManager.AppSettings["accountId"];
                string consumerKey = ConfigurationManager.AppSettings["consumerKey"];
                string consumerSecret = ConfigurationManager.AppSettings["consumerSecret"];
                string tokenId = ConfigurationManager.AppSettings["tokenId"];
                string tokenSecret = ConfigurationManager.AppSettings["tokenSecret"];

                string baseUrl =
                    $"https://{accountId}.restlets.api.netsuite.com/app/site/hosting/restlet.nl";

                // ===== Query Params =====
                var queryParams = new Dictionary<string, string>
        {
            { "script", script.ToString() },
            { "deploy", "1" },
            { "parentFolderName", platform },
            { "folderName", SKUFolder }
        };

                // ===== OAuth Params =====
                var oauthParams = new Dictionary<string, string>
        {
            { "oauth_consumer_key", consumerKey },
            { "oauth_token", tokenId },
            { "oauth_nonce", Guid.NewGuid().ToString("N") },
            { "oauth_timestamp", ((int)(DateTime.UtcNow -
                new DateTime(1970, 1, 1)).TotalSeconds).ToString() },
            { "oauth_signature_method", "HMAC-SHA256" },
            { "oauth_version", "1.0" }
        };

                // ===== Combine params for signing =====
                var allParams = queryParams
                    .Concat(oauthParams)
                    .ToDictionary(k => k.Key, v => v.Value);

                var normalizedParams = string.Join("&",
                    allParams
                        .Select(kv => $"{UrlEncode(kv.Key)}={UrlEncode(kv.Value)}")
                        .OrderBy(x => x));

                string signatureBase =
                    $"GET&{UrlEncode(baseUrl)}&{UrlEncode(normalizedParams)}";

                string signingKey =
                    $"{UrlEncode(consumerSecret)}&{UrlEncode(tokenSecret)}";

                string signature;
                using (var hmac = new HMACSHA256(Encoding.ASCII.GetBytes(signingKey)))
                {
                    signature = Convert.ToBase64String(
                        hmac.ComputeHash(Encoding.ASCII.GetBytes(signatureBase)));
                }

                // ===== Authorization Header =====
                var authHeader = new StringBuilder();
                authHeader.Append($"OAuth realm=\"{accountId}\",");

                foreach (var kv in oauthParams)
                    authHeader.Append($"{kv.Key}=\"{UrlEncode(kv.Value)}\",");

                authHeader.Append($"oauth_signature=\"{UrlEncode(signature)}\"");

                // ===== Request URL =====
                var requestUrl = baseUrl + "?" +
                    string.Join("&",
                        queryParams.Select(kv =>
                            $"{kv.Key}={UrlEncode(kv.Value)}"));

                // ===== HTTP Call =====
                //using var client = new HttpClient();
                //using var request = new HttpRequestMessage(HttpMethod.Get, requestUrl);

                //request.Headers.Add("Authorization", authHeader.ToString());
                //request.Headers.Add("Accept", "application/json");

                //var response = await client.SendAsync(request);
                //var body = await response.Content.ReadAsStringAsync();

                //if (!response.IsSuccessStatusCode)
                //    throw new Exception(body);

                //dynamic obj = JsonConvert.DeserializeObject(body);

                //if (obj?.success != true || obj.results == null)
                //    return null;

                //// ===== Build image list =====
                //var skuImages = new List<NsSkuImage>();

                //foreach (var sku in obj.results)
                //{
                //    if (sku.images == null) continue;

                //    foreach (var image in sku.images)
                //    {
                //        skuImages.Add(new NsSkuImage
                //        {
                //            SkuCode = sku.skuFolder,
                //            ImageId = image.id.ToString(),
                //            ImageName = image.name,
                //            Type = image.type,
                //            Platform = platform,
                //            CreatedAt = DateTime.UtcNow
                //        });
                //    }
                //}

                using (HttpClient client = new HttpClient())
                using (HttpRequestMessage request =
                    new HttpRequestMessage(HttpMethod.Get, requestUrl.ToString()))
                {
                    request.Headers.Add("Authorization", authHeader.ToString());
                    request.Headers.Add("Accept", "application/json");

                    HttpResponseMessage response = await client.SendAsync(request);
                    string body = await response.Content.ReadAsStringAsync();

                    if (!response.IsSuccessStatusCode)
                        return null;


                    var apiResponse =
    JsonConvert.DeserializeObject<NsSkuImageResponse>(body);

                    if (apiResponse == null || apiResponse.success != true || apiResponse.image == null)
                        return null;

                    // Build SINGLE image object (domain model)
                    NsSkuImage skuImage = new NsSkuImage
                    {
                        SkuCode = apiResponse.folder,
                        ImageId = apiResponse.image.id,
                        ImageName = apiResponse.image.name,
                        Type = apiResponse.image.type,
                        Platform = apiResponse.parentFolder,
                        CreatedAt = DateTime.UtcNow
                        // SinceDate intentionally omitted unless you actually have it
                    };

                    if (skuImage == null)
                        return null;

                    using (var context = new LinkboxDb())
                    {
                        // ===== CHECK EXISTING (DO NOT INSERT IF EXISTS) =====
                        var existingFileId = context.FileCabinetImages
                            .Where(x => x.FileID == skuImage.ImageId && x.SKUFolder == skuImage.SkuCode)
                            .Select(x => x.FileID)
                            .FirstOrDefault();

                        if (!string.IsNullOrEmpty(existingFileId))
                            return existingFileId;

                        // ===== INSERT SINGLE IMAGE =====
                        var entity = new FileCabinetImage
                        {
                            FileID = skuImage.ImageId,
                            SKUFolder = skuImage.SkuCode,
                            ImageName = skuImage.ImageName,
                            Type = skuImage.Type,
                            CreatedDate = skuImage.CreatedAt
                        };

                        context.FileCabinetImages.Add(entity);
                        context.SaveChanges();

                        return entity.FileID;
                    }

                }
            }
            catch (Exception ex)
            {
                throw;
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
        private static object GetFieldValueDynamic(object obj, string fieldName)
        {
            if (obj == null || string.IsNullOrWhiteSpace(fieldName))
                return null;

            // Case-insensitive property lookup
            var prop = obj.GetType().GetProperty(
                fieldName,
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);

            if (prop == null)
                return null;

            return prop.GetValue(obj);
        }
        public static string ExtractVariantsOnlyJson(string productJson)
        {
            using (var doc = JsonDocument.Parse(productJson))
            {
                // expects root.product.variants
                var variants = doc.RootElement
                    .GetProperty("product")
                    .GetProperty("variants");

                // return JSON array as string
                return variants.GetRawText();
            }
        }

    }
    public class NsSkuImage
    {
        public int Id { get; set; }

        public string SkuCode { get; set; }
        public string ImageId { get; set; }
        public string Platform { get; set; }
        public string ImageName { get; set; }
        public string Type { get; set; }

        public DateTime SinceDate { get; set; }
        public DateTime CreatedAt { get; set; }
    }
    public class NsSkuImageResponse
    {
        public bool success { get; set; }
        public string parentFolder { get; set; }
        public string folder { get; set; }
        public NsSkuImageResponseImage image { get; set; }
    }
    public class NsSkuImageResponseImage
    {
        public string id { get; set; }
        public string name { get; set; }
        public string type { get; set; }
        public string lastModified { get; set; }
    }

    public class ImageDetailsResponse
    {
        public bool success { get; set; }
        public int fileId { get; set; }
        public string name { get; set; }
        public string mimeType { get; set; }
        public string base64 { get; set; }
    }

    public class ShopifyApiResult
    {
        public int StatusCode { get; set; }
        public bool IsSuccess { get; set; }
        public string Body { get; set; } = "";
        public string ReasonPhrase { get; set; } = "";
        public string DefaultVariantId { get; set; } = null;
        public string DefaultProductId { get; set; } = null;

    }


    public class ZaloraApiResult
    {
        public int StatusCode { get; set; }
        public bool IsSuccess { get; set; }
        public string Body { get; set; } = "";
        public string ReasonPhrase { get; set; } = "";
        public string jsonBody { get; set; } = "";
        public string duplicate_Id { get; set; }

    }
    public class TiktokApiResult
    {   
        public int StatusCode { get; set; }
        public bool IsSuccess { get; set; }
        public string Body { get; set; } = "";
        public string ReasonPhrase { get; set; } = "";
        public string jsonBody { get; set; } = "";
        public string ErrrorCode { get; set; }
        public string ErrorMessage { get; set; }
    }

}
