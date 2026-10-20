using DocumentFormat.OpenXml.Spreadsheet;
using DomainLayer.Models.Shopify;
using LinkBoxUI.Context;
using Microsoft.Graph.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using System.Web.Http;

namespace LinkBoxUI.Helpers
{
    public class ShopifyHelper
    {

        LinkboxDb _context = new LinkboxDb();
        public async Task<string> CreateFulfillment(string order_reference)
        {
            try
            {
                var APISetup = _context.APISetups
               .FirstOrDefault(x => x.APICode == "ShopifyAPI");

                string accessToken = APISetup.APIToken;
                string url = $"{APISetup.APIURL}/orders/{order_reference}/fulfillment_orders.json";

                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("X-Shopify-Access-Token", accessToken);
                    client.DefaultRequestHeaders.Accept.Add(
                        new MediaTypeWithQualityHeaderValue("application/json")
                    );

                    var response = await client.GetAsync(url);
                    string json = await response.Content.ReadAsStringAsync();

                    if (!response.IsSuccessStatusCode)
                        throw new Exception($"Shopify error: {response.StatusCode} - {response.ReasonPhrase}");

                    var obj = JsonConvert.DeserializeObject<FulfillmentOrderModel>(json);

                    // safely extract the first ID and convert to string
                    var id = obj?.fulfillment_orders?.FirstOrDefault()?.id;

                    return id?.ToString(); // returns null if no ID
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error Retrieving fulfillment order id: {ex.Message}");
            }
           
        }
        public async Task<string> PostFulfillment(string payload)
        {
            try
            {
                var APISetup = _context.APISetups
               .FirstOrDefault(x => x.APICode == "ShopifyAPI");

                string accessToken = APISetup.APIToken;
                string url = $"{APISetup.APIURL}/fulfillments.json";
                var content = new StringContent(payload, Encoding.UTF8, "application/json");
                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("X-Shopify-Access-Token", accessToken);

                    var response = await client.PostAsync(url, content);
                    string json = await response.Content.ReadAsStringAsync();
                    if(json.Contains("status= closed"))
                    {
                        return json;
                    }
                    if (!response.IsSuccessStatusCode)
                        throw new Exception($"Shopify Fulfillment Error: {json}");

                    return json;
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error Retrieving fulfillment order id: {ex.Message}");
            }

        }

        public async Task<string> MainProduct_CallShopifyGraphQLAsync(string sku)
        {
            try
            {
                var APISetup = _context.APISetups
              .FirstOrDefault(x => x.APICode == "ShopifyAPI");

                string accessToken = APISetup.APIToken;
                var shop = "strokes-beauty";
                var url = $"https://{shop}.myshopify.com/admin/api/2026-01/graphql.json";

                var requestBody = new
                {
                    query = @"
                query GetProductBySku($query: String!) {
                  products(first: 1, query: $query) {
                    edges {
                      node {
                        id
                        title
                        featuredImage {
                          id
                          url
                          altText
                        }
                      }
                    }
                  }
                }",
                    variables = new
                    {
                        query = $"sku:{sku}"
                    }
                };

                var jsonBody = JsonConvert.SerializeObject(requestBody);
                using (var content = new StringContent(jsonBody, Encoding.UTF8, "application/json"))
                //var content = new StringContent(requestBody, Encoding.UTF8, "application/json");
                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("X-Shopify-Access-Token", accessToken);

                    var response = await client.PostAsync(url, content);
                    string json = await response.Content.ReadAsStringAsync();

                    if (!response.IsSuccessStatusCode)
                        throw new Exception($"Shopify CallShopifyGraphQLAsync Error: {json}");

                    return json;
                }

            }
            catch (Exception ex)
            {
                throw new Exception($"Error Calling Shopify GraphQL: {ex.Message}");
            }
        }



        public async Task<string> CallShopifyGraphQLAsync(string sku)
        {
            try
            {
                var APISetup = _context.APISetups
              .FirstOrDefault(x => x.APICode == "ShopifyAPI");

                string accessToken = APISetup.APIToken;
                var shop = "strokes-beauty";
                var url = $"https://{shop}.myshopify.com/admin/api/2026-01/graphql.json";

                var requestBody = new
                {
                    query = @"
            query GetVariantBySku($query: String!) {
              productVariants(first: 1, query: $query) {
                edges {
                  node {
                    availableForSale
                    id
                    sku
                    title
                    inventoryQuantity
                    sellableOnlineQuantity
                    price
                    inventoryItem {
                              id
                              tracked
                            }
                    product {
                      id
                      title
                      totalInventory
                    }

                    image {
                      id
                      url
                      altText
                    }
                  }
                }
              }
            }",
                    variables = new
                    {
                        query = $"sku:{sku}" // ✅ dynamic SKU input
                    }
                };


                var jsonBody = JsonConvert.SerializeObject(requestBody);
                using (var content = new StringContent(jsonBody, Encoding.UTF8, "application/json"))
                //var content = new StringContent(requestBody, Encoding.UTF8, "application/json");
                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("X-Shopify-Access-Token", accessToken);

                    var response = await client.PostAsync(url, content);
                    string json = await response.Content.ReadAsStringAsync();
                   
                    if (!response.IsSuccessStatusCode)
                        throw new Exception($"Shopify CallShopifyGraphQLAsync Error: {json}");

                    return json;
                }

            }
            catch (Exception ex)
            {
                throw new Exception($"Error Calling Shopify GraphQL: {ex.Message}");
            }
           
          
        }
        public async Task<string> Verify_Parent(string sku)
        {
            try
            {
                var APISetup = _context.APISetups
              .FirstOrDefault(x => x.APICode == "ShopifyAPI");

                string accessToken = APISetup.APIToken;
                var shop = "strokes-beauty";
                var url = $"https://{shop}.myshopify.com/admin/api/2026-01/graphql.json";

                var requestBody = new
                {
                    query = @"
            query GetVariantBySku($query: String!) {
              productVariants(first: 1, query: $query) {
                edges {
                  node {
                    availableForSale
                    id
                    sku
                    title
                    product {
                      id
                      title
                      
                    }

                  }
                }
              }
            }",
                    variables = new
                    {
                        query = $"sku:{sku}" // ✅ dynamic SKU input
                    }
                };


                var jsonBody = JsonConvert.SerializeObject(requestBody);
                using (var content = new StringContent(jsonBody, Encoding.UTF8, "application/json"))
                //var content = new StringContent(requestBody, Encoding.UTF8, "application/json");
                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("X-Shopify-Access-Token", accessToken);

                    var response = await client.PostAsync(url, content);
                    string json = await response.Content.ReadAsStringAsync();

                    if (!response.IsSuccessStatusCode)
                        throw new Exception($"Shopify CallShopifyGraphQLAsync Error: {json}");

                    return json;
                }

            }
            catch (Exception ex)
            {
                throw new Exception($"Error Calling Shopify GraphQL: {ex.Message}");
            }


        }
        public Dictionary<string, string> ExtractIds(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                throw new ArgumentException("Shopify response JSON is empty.");

            using (JsonDocument doc = JsonDocument.Parse(json))
            {
                JsonElement root = doc.RootElement;

                // Navigate: data → productVariants → edges[0] → node
                if (!root.TryGetProperty("data", out JsonElement data))
                    throw new InvalidOperationException("Missing 'data' node in Shopify response.");

                if (!data.TryGetProperty("productVariants", out JsonElement productVariants))
                    throw new InvalidOperationException("Missing 'productVariants' node in Shopify response.");

                if (!productVariants.TryGetProperty("edges", out JsonElement edges) ||
                    edges.ValueKind != JsonValueKind.Array ||
                    edges.GetArrayLength() == 0)
                    throw new InvalidOperationException("No productVariants returned from Shopify.");

                JsonElement firstEdge = edges[0];

                if (!firstEdge.TryGetProperty("node", out JsonElement node))
                    throw new InvalidOperationException("Missing 'node' in productVariants edge.");

                // Variant ID (REQUIRED)
                if (!node.TryGetProperty("id", out JsonElement variantIdElem))
                    throw new InvalidOperationException("VariantId is missing from Shopify response.");

                string variantGid = variantIdElem.GetString();

                // Product ID (REQUIRED)
                if (!node.TryGetProperty("product", out JsonElement productObj) ||
                    !productObj.TryGetProperty("id", out JsonElement productIdElem))
                    throw new InvalidOperationException("ProductId is missing from Shopify response.");

                string productGid = productIdElem.GetString();

                // Inventory Item ID (REQUIRED)
                if (!node.TryGetProperty("inventoryItem", out JsonElement inventoryItemObj) ||
                    !inventoryItemObj.TryGetProperty("id", out JsonElement inventoryItemIdElem))
                    throw new InvalidOperationException(
                        "InventoryItemId is missing. Ensure inventoryItem { id } is included and inventory is tracked."
                    );

                string inventoryItemGid = inventoryItemIdElem.GetString();

                // Image ID (REQUIRED)

                string imageGid = "";

                if (node.TryGetProperty("image", out JsonElement imageObj) &&
     imageObj.ValueKind == JsonValueKind.Object &&
     imageObj.TryGetProperty("id", out JsonElement imageIdElem) &&
     imageIdElem.ValueKind == JsonValueKind.String)
                {
                    imageGid = imageIdElem.GetString();
                }

                // Final safety check
                if (string.IsNullOrEmpty(variantGid) ||
                    string.IsNullOrEmpty(productGid) ||
                    string.IsNullOrEmpty(inventoryItemGid)
                    //||  string.IsNullOrEmpty(imageGid)
                    )
                {
                    throw new InvalidOperationException("One or more required IDs are empty.");
                }

                // Single, deterministic result
                return new Dictionary<string, string>
        {
            { "VariantId", ExtractNumericId(variantGid) },
            { "ProductId", ExtractNumericId(productGid) },
            { "InventoryItemId", ExtractNumericId(inventoryItemGid) },
            { "ImageId", ExtractNumericId(imageGid) }
        };
            }
        }

        public Dictionary<string, string> ExtractProductId(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                throw new ArgumentException("Shopify response JSON is empty.");

            using (JsonDocument doc = JsonDocument.Parse(json))
            {
                JsonElement root = doc.RootElement;

                // Navigate: data → productVariants → edges[0] → node
                if (!root.TryGetProperty("data", out JsonElement data))
                    throw new InvalidOperationException("Missing 'data' node in Shopify response.");

                if (!data.TryGetProperty("productVariants", out JsonElement productVariants))
                    throw new InvalidOperationException("Missing 'productVariants' node in Shopify response.");

                if (!productVariants.TryGetProperty("edges", out JsonElement edges) ||
                    edges.ValueKind != JsonValueKind.Array ||
                    edges.GetArrayLength() == 0)
                    throw new InvalidOperationException("No productVariants returned from Shopify.");

                JsonElement firstEdge = edges[0];

                if (!firstEdge.TryGetProperty("node", out JsonElement node))
                    throw new InvalidOperationException("Missing 'node' in productVariants edge.");


                // Product ID (REQUIRED)
                if (!node.TryGetProperty("product", out JsonElement productObj) ||
                    !productObj.TryGetProperty("id", out JsonElement productIdElem))
                    throw new InvalidOperationException("ProductId is missing from Shopify response.");

                string productGid = productIdElem.GetString();

               

                // Image ID (REQUIRED)

             

                // Single, deterministic result
                return new Dictionary<string, string>
        {
           
            { "ProductId", ExtractNumericId(productGid) },
           
        };
            }
        }
        public bool ShopifyProductExists(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return false;

            try
            {
                using (JsonDocument doc = JsonDocument.Parse(json))
                {
                    JsonElement root = doc.RootElement;

                    // 1) REST-style: { "product": { "variants": [ ... ] } }
                    if (root.TryGetProperty("product", out JsonElement product))
                    {
                        if (product.TryGetProperty("variants", out JsonElement variants) &&
                            variants.ValueKind == JsonValueKind.Array &&
                            variants.GetArrayLength() > 0)
                        {
                            return true;
                        }
                    }

                    // 2) GraphQL variant query: { "data": { "productVariants": { "edges": [ ... ] } } }
                    if (root.TryGetProperty("data", out JsonElement data))
                    {
                        if (data.TryGetProperty("productVariants", out JsonElement productVariants) &&
                            productVariants.TryGetProperty("edges", out JsonElement edgesPV) &&
                            edgesPV.ValueKind == JsonValueKind.Array &&
                            edgesPV.GetArrayLength() > 0)
                        {
                            return true;
                        }

                        // 3) GraphQL product query: { "data": { "products": { "edges": [ { "node": { "variants": [...] } } ] } } }
                        if (data.TryGetProperty("products", out JsonElement products) &&
                            products.TryGetProperty("edges", out JsonElement edgesP) &&
                            edgesP.ValueKind == JsonValueKind.Array &&
                            edgesP.GetArrayLength() > 0)
                        {
                            // If product node contains variants array, check its length
                            for (int i = 0; i < edgesP.GetArrayLength(); i++)
                            {
                                var edge = edgesP[i];
                                if (edge.TryGetProperty("node", out JsonElement node) &&
                                    node.TryGetProperty("variants", out JsonElement nodeVariants) &&
                                    nodeVariants.ValueKind == JsonValueKind.Array &&
                                    nodeVariants.GetArrayLength() > 0)
                                {
                                    return true;
                                }
                            }

                            // edges exist → treat as product present (useful when variants are nested differently)
                            return true;
                        }
                    }

                    return false;
                }
            }
            catch
            {
                // If JSON is invalid or unexpected, return false (or consider logging)
                return false;
            }
        }


        public async Task<bool>CallSetInventoryLevel(string sku,string TotalQuantity)
        {
            try
            {
               var qtty= int.TryParse(TotalQuantity, out var qty) ? qty : 0;
                if (qtty == 0)
                    return true;
                string skuCode = sku;//netsuite.itemid.ToString();
                int totalquantityonhand = int.Parse(TotalQuantity.ToString());
                var result = await CallShopifyGraphQLAsync(skuCode);
                //var sample = await shopifyHelper.MainProduct_CallShopifyGraphQLAsync(skuCode);
                dynamic ids = ExtractIds(result);
                string z_productId = null;
                string z_variantId = null;
                string z_inventoryId = null;
                string z_imageId = null;


                if (ids != null)
                {   //Patch Variant
                    z_productId = ids["ProductId"];
                    z_variantId = ids["VariantId"];
                    z_inventoryId = ids["InventoryItemId"];
                    z_imageId = ids["ImageId"];
                }
                var inv_payload = new
                {
                    location_id = 52564852903L,
                    inventory_item_id = z_inventoryId,
                    available = totalquantityonhand,
                    inventory_management = "shopify"
                };
                var payloadJson = System.Text.Json.JsonSerializer.Serialize(inv_payload);

                string respo = await SetInventoryLevelAsync(z_inventoryId, totalquantityonhand);
                
                if (respo.Contains("error"))
                {
                    _context.SystemLogs.Add(new DomainLayer.Models.SystemLog
                    {
                        Task = $"SetInventoryLevelAsync Shopify Product Inventory Update",
                        ApiUrl = "https://strokes-beauty.myshopify.com/admin/api/2025-01/inventory_levels/set.json",
                        Json = payloadJson,
                        ErrorMsg = respo,
                        CreateDate = DateTime.Now,
                    });
                }
                return true;
            }
            catch (Exception ex)
            {
                _context.SystemLogs.Add(new DomainLayer.Models.SystemLog
                {
                    Task = $"Line : 493 - CallSetInventoryLevel ",
                    ApiUrl = "https://strokes-beauty.myshopify.com/admin/api/2025-01/inventory_levels/set.json",
                 
                    ErrorMsg = ex.Message,
                    CreateDate = DateTime.Now,
                });
                return false;
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
                available = available,
                inventory_management= "shopify"
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


        private static string ExtractNumericId(string gid)
        {
            if (string.IsNullOrEmpty(gid))
                return null;

            string[] parts = gid.Split('/');
            return (parts != null && parts.Length > 0) ? parts[parts.Length - 1] : null;
        }


    }
}