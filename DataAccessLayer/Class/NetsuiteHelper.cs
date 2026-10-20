using DocumentFormat.OpenXml.Office2010.Excel;
using DocumentFormat.OpenXml.Office2010.ExcelAc;
using DomainLayer.Models;
using DomainLayer.Models.Shopify;
using DomainLayer.ViewModels;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Results;
using System.Windows.Forms;
namespace DataAccessLayer.Class
{
    public class NetsuiteHelper
    {
        public async Task<string> GetNetSuiteData(string endpoint, string query)
        {
            // Call the API and get the response
            var response = await CallQueryServiceAsync(endpoint, query);

            var Column_Value = "";
            if (response.ResponseMessage.Contains("Query executed successfully"))
            {
                var apiResponse = JsonConvert.DeserializeObject<JsonResponseViewModel>(response.ResponseMessage);
                if (apiResponse != null)
                {

                    if (!apiResponse.Error)
                    {
                        if (apiResponse.Data?.Items != null)
                        {
                            foreach (var item in apiResponse.Data.Items)
                            {
                                // Iterate over the properties of each item using reflection
                                foreach (var property in item.GetType().GetProperties())
                                {
                                    var value = property.GetValue(item);

                                    // Check if the value is not null or empty (for string properties)
                                    if (value != null && !(value is string str && string.IsNullOrWhiteSpace(str)))
                                    {
                                        // Print the property name and its value
                                        Console.WriteLine($"{property.Name}: {value}");
                                        Column_Value = value.ToString();
                                        break;
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        throw new Exception(apiResponse.Message.ToString());
                    }

                }
                else
                {
                    Console.WriteLine("Failed to deserialize the response.");
                }

            }
            else
            {
                throw new Exception(response.ResponseMessage.ToString());
            }

            return Column_Value;
        }
        public async Task<List<string>> GetListNetSuiteData(string endpoint, string query)
        {
            // Call the API and get the response
            var response = await CallQueryServiceAsync(endpoint, query);

            var columnValues = new List<string>();  // List to store values

            if (response.ResponseMessage.Contains("Query executed successfully"))
            {
                var apiResponse = JsonConvert.DeserializeObject<JsonResponseViewModel>(response.ResponseMessage);
                if (apiResponse != null)
                {
                    if (!apiResponse.Error)
                    {
                        if (apiResponse.Data?.Items != null)
                        {
                            foreach (var item in apiResponse.Data.Items)
                            {
                                // Iterate over the properties of each item using reflection
                                foreach (var property in item.GetType().GetProperties())
                                {
                                    var value = property.GetValue(item);

                                    // Check if the value is not null or empty (for string properties)
                                    if (value != null && !(value is string str && string.IsNullOrWhiteSpace(str)))
                                    {
                                        // Add the property value to the list
                                        columnValues.Add(value.ToString());
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        throw new Exception(apiResponse.Message.ToString());
                    }
                }
                else
                {
                    Console.WriteLine("Failed to deserialize the response.");
                }
            }
            else
            {
                throw new Exception(response.ResponseMessage.ToString());
            }

            return columnValues;
        }


        public async Task<List<NetsuiteItemModel>> GetNetsuiteJson(string endpoint,string query)
        {
            // Call the API and get the response

            var response = await CallQueryServiceAsync(endpoint, query);

            if (!response.ResponseMessage.Contains("Query executed successfully"))
                throw new Exception(response.ResponseMessage);

            var apiResponse = JsonConvert.DeserializeObject<JsonResponseViewModel>(response.ResponseMessage);
            if (apiResponse == null)
                throw new Exception("Failed to deserialize the response.");

            if (apiResponse.Error)
                throw new Exception(apiResponse.Message.ToString());

            var result = new List<NetsuiteItemModel>();

            if (apiResponse.Data?.Items != null)
            {
                foreach (var item in apiResponse.Data.Items)
                {
                    var map = new NetsuiteItemModel
                    {
                        id = item.id?.ToString(),
                        itemid = item.itemid?.ToString(),
                        displayname = item.displayname?.ToString(),
                        description = item.description?.ToString(),
                        skucode = item.custitem_skucode?.ToString(),
                        sku_baseprice = item.base_price?.ToString(),
                        createddate = item.createddate?.ToString(),
                        lastmodifieddate = item.lastmodifieddate?.ToString(),
                        totalquantityonhand = item.totalquantityonhand?.ToString() ?? "0",
                        parent = item.parent?.ToString(),
                        custitem_dbti_title = item.custitem_dbti_title?.ToString(),
                        custitem_dbti_description = item.custitem_dbti_description?.ToString(),
                        product_id = item.product_id?.ToString(),
                        variant_id = item.variant_id?.ToString(),
                        custitem_suvariation = item.custitem_suvariation?.ToString(),
                        custitem_sucategry = item.custitem_sucategry?.ToString(),
                        custitem_subcategory = item.custitem_subcategory?.ToString(),
                        custitem_dbti_item_display_image = item.custitem_dbti_item_display_image?.ToString(),
                        category = item.category?.ToString(),
                        catalogtype = item.catalogtype?.ToString(),
                        subcategory = item.subcategory?.ToString(),
                        variation = item.variation?.ToString(),
                        gender = item.gender?.ToString(),
                        brand = item.brand?.ToString(),
                        image_ids = item.image_ids?.ToString(),
                        weight = item.weight?.ToString(),
                        pkg_weight = item.pkg_weight?.ToString(),
                        pkg_length = item.pkg_length?.ToString(),
                        weight_uom = item.weight_uom?.ToString(),
                        color_family = item.color_family?.ToString(),
                        product_id_v2 = item.product_id_v2?.ToString(),
                        child_image = item.child_image?.ToString(),
                        shopee_id = item.shopee_id?.ToString(),
                        custrecord_dbti_product_title = item.custrecord_dbti_product_title?.ToString()

                    };

                    result.Add(map);
                }
            }

            return result;
        }

        public async Task<DataTable> GetNetsuiteDataTable(string endpoint, string query)
        {
            // Step 1: Fetch the JSON data
            var itemList = await GetNetsuiteJson(endpoint, query);

            // Step 2: Convert List to DataTable
            DataTable dataTable = new DataTable();

            if (itemList.Count > 0)
            {
                // Create columns based on the properties of NetsuiteItemModel
                dataTable.Columns.Add("Id", typeof(int));
                dataTable.Columns.Add("SKU", typeof(string));
                dataTable.Columns.Add("ProductId", typeof(string));

                // Add more columns as needed based on your model's properties

                // Add rows to the DataTable
                foreach (var item in itemList)
                {
                    dataTable.Rows.Add(item.id, item.itemid,item.product_id);
                    // Add other properties to the row as needed
                }
            }

            return dataTable;
        }




        public async Task<List<fulfillmentNetsuiteModel>> ListGetNetSuiteData(string endpoint, string query)
        {
            var response = await CallQueryServiceAsync(endpoint, query);

            if (!response.ResponseMessage.Contains("Query executed successfully"))
                throw new Exception(response.ResponseMessage);

            var apiResponse = JsonConvert.DeserializeObject<JsonResponseViewModel>(response.ResponseMessage);
            if (apiResponse == null)
                throw new Exception("Failed to deserialize the response.");

            if (apiResponse.Error)
                throw new Exception(apiResponse.Message.ToString());

            var result = new List<fulfillmentNetsuiteModel>();

            if (apiResponse.Data?.Items != null)
            {
                foreach (var item in apiResponse.Data.Items)
                {
                    var map = new fulfillmentNetsuiteModel
                    {
                        id = item.id?.ToString(),
                        ordernumber = item.ordernumber?.ToString(),
                        status = item.status?.ToString()
                    };

                    result.Add(map);
                }
            }

            return result;
        }


        public async Task<Dictionary<int, string>> GetDictionaryNetSuiteData(string endpoint, string query)
        {
            var response = await CallQueryServiceAsync(endpoint, query);
            var resultDictionary = new Dictionary<int, string>();

            if (response.ResponseMessage.Contains("Query executed successfully"))
            {
                var apiResponse = JsonConvert.DeserializeObject<JsonResponseViewModel>(response.ResponseMessage);
                if (apiResponse != null)
                {
                    if (!apiResponse.Error && apiResponse.Data?.Items != null)
                    {
                        foreach (var item in apiResponse.Data.Items)
                        {
                            int id = 0;
                            string fullName = null;

                            foreach (var property in item.GetType().GetProperties())
                            {
                                var value = property.GetValue(item);

                                if (property.Name.ToLower() == "id" && value != null)
                                {
                                    id = int.Parse(value.ToString());
                                    
                                }
                                if (property.Name.ToLower() == "fullname" && value != null)
                                {
                                    fullName = value.ToString().Replace("Bank - Others :", "").Trim();
                                }
                            }

                            if (id > 0)
                            {
                                resultDictionary[id] = fullName ?? string.Empty;
                            }
                        }
                    }
                    else
                    {
                        throw new Exception(apiResponse.Message.ToString());
                    }
                }
                else
                {
                    Console.WriteLine("Failed to deserialize the response.");
                }
            }
            else
            {
                throw new Exception(response.ResponseMessage.ToString());
            }

            return resultDictionary;
        }


        public async Task<string> PostDataToNetSuite(string endpoint, string json,string module, string method = null, string id = null)
        {
            var (isSuccess, responseMessage) = await CallNetsuitePosting(endpoint, json, module, method, id);

            return isSuccess ? responseMessage : $"Failed to post data: {responseMessage}";
        }

        public async Task<(bool IsSuccess, string ResponseMessage)> CallQueryServiceAsync(string url, string query)
        {
            try
            {
                using (var handler = new HttpClientHandler())
                {
                    // Ignore SSL certificate validation issues (use with caution in production)
                    handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true;

                    using (var client = new HttpClient(handler))
                    {
                        // Serialize the query into JSON
                        var jsonContent = JsonConvert.SerializeObject(query);

                        // Prepare the content for the POST request
                        var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                        // Send the POST request
                        var response = await client.PostAsync(url, content);
                        
                        var responseData = await response.Content.ReadAsStringAsync();

                        if (!response.IsSuccessStatusCode)
                        {
                            return (false, $"Error: {response.StatusCode}, Response: {responseData}");
                        }

                        return (true, responseData);
                    }
                }
            }
            catch (Exception ex)
            {
                return (false, $"Exception: {ex.Message}");
            }
        }


        public async Task<(bool IsSuccess, string ResponseMessage)> CallNetsuitePosting(string url, string query,string ModuleName, string method = "POST", string id = null)
        {
            try
            {
                using (var handler = new HttpClientHandler())
                {
                    // Ignore SSL certificate validation issues (use with caution in production)
                    handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true;

                    using (var client = new HttpClient(handler))
                    {
                        var requestData = new
                        {
                            query = query,
                            module = ModuleName,
                            method = method,
                            id = id
                        };

                        var jsonContent = JsonConvert.SerializeObject(requestData);

                        // Prepare the content for the POST request
                        var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                        // Send the POST request
                        var response = await client.PostAsync(url, content);
                        var responseData = await response.Content.ReadAsStringAsync();

                        if (!response.IsSuccessStatusCode)
                        {
                            return (false, $"Error: {response.StatusCode}, Response: {responseData}");
                        }

                        return (true, responseData);
                    }
                }
            }
            catch (Exception ex)
            {
                return (false, $"Exception: {ex.Message}");
            }
        }
    }
}
