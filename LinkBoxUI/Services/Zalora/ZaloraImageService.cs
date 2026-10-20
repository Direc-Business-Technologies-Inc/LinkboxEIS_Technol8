using Microsoft.AspNetCore.WebUtilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;

namespace LinkBoxUI.Services.Zalora
{
    public class ZaloraImageService
    {
        private readonly HttpClient _httpClient;

        public ZaloraImageService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }
        public async Task<List<ImageResponseModel>> GetImagesAsync(
            string sku,
            IEnumerable<string> internalIds)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(sku))
                    throw new ArgumentException("SKU is required.", nameof(sku));

                if (internalIds == null || !internalIds.Any())
                    throw new ArgumentException("At least one internalId is required.", nameof(internalIds));

                // Build query parameters
                var queryParams = new Dictionary<string, string>
                {
                    { "sku", sku }
                };

                var url = QueryHelpers.AddQueryString(
                   "https://localhost:7242/api/Zalora/images",
                    queryParams);

                //"https://localhost:7242/api/Zalora/images", Local
                //https://159.138.99.111:8085/api/Zalora/images", Live
                // Add multiple internalIds
                var uriBuilder = new StringBuilder(url);

                foreach (var id in internalIds)
                {
                    uriBuilder.Append($"&internalIds={Uri.EscapeDataString(id)}");
                }

                using (var handler = new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
                })
                using (var httpClient = new HttpClient(handler))
                {
                    //var response = await _httpClient.GetAsync(uriBuilder.ToString());
                    var response = await httpClient.GetAsync(uriBuilder.ToString());
                    var raw = await response.Content.ReadAsStringAsync();

                    if (!response.IsSuccessStatusCode)
                    {
                        throw new Exception($"API Error: {response.StatusCode} - {raw}");
                    }

                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    };

                    var result = JsonSerializer.Deserialize<GetImagesResponse>(raw, options);

                    return result?.Items;
                }

                

            }
            catch(Exception ex)
            {
                throw new Exception($"GetImagesAsync - Error {ex}");
            }
        }
    }
    public class GetImagesResponse
    {
        public string Sku { get; set; }
        public List<ImageResponseModel> Items { get; set; }
    }

    public class ImageResponseModel
    {
        public string InternalId { get; set; }
        public string SavedFileName { get; set; }
        public string DisplayUrl { get; set; }
        public string Error { get; set; }
    }
}