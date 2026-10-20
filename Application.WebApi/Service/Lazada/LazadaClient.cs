using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Application.WebApi.Helpers.Lazada;
using Microsoft.Extensions.Configuration;

namespace Application.WebApi.Service.Lazada
{
    public class LazadaClient
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _config;

        private readonly string _appKey;
        private readonly string _appSecret;
        private readonly string _baseUrl;
        private readonly string _authUrl;

        public LazadaClient(HttpClient httpClient, IConfiguration config)
        {
            _httpClient = httpClient;
            _config = config;

            _appKey = config["Lazada:AppKey"];
            _appSecret = config["Lazada:AppSecret"];
            _baseUrl = config["Lazada:BaseUrl"];
            _authUrl = config["Lazada:AuthUrl"];
        }

        // Create Access Token
        public async Task<JsonDocument> CreateAccessTokenAsync(string oauthCode)
        {
            var apiPath = "/auth/token/create";

            var parameters = new Dictionary<string, string>
            {
                { "app_key",     _appKey    },
                { "code",        oauthCode  },
                { "sign_method", "sha256"   },
                { "timestamp",   LazadaSignatureHelper.GetTimestamp() }
            };

            parameters["sign"] = LazadaSignatureHelper.GenerateSignature(_appSecret, apiPath, parameters);

            var url = BuildUrl(_authUrl, apiPath, parameters);
            return await SendGetAsync(url);
        }

        // Refresh Access Token
        public async Task<JsonDocument> RefreshAccessTokenAsync(string refreshToken)
        {
            var apiPath = "/auth/token/refresh";

            var parameters = new Dictionary<string, string>
            {
                { "app_key",       _appKey       },
                { "refresh_token", refreshToken  },
                { "sign_method",   "sha256"      },
                { "timestamp",     LazadaSignatureHelper.GetTimestamp() }
            };

            parameters["sign"] = LazadaSignatureHelper.GenerateSignature(_appSecret, apiPath, parameters);

            var url = BuildUrl(_authUrl, apiPath, parameters);
            return await SendGetAsync(url);
        }

        // Get Products
        public async Task<JsonDocument> GetProductsAsync(string accessToken, string filter = "live", int limit = 10)
        {
            var apiPath = "/products/get";

            var parameters = new Dictionary<string, string>
            {
                { "app_key",      _appKey          },
                { "access_token", accessToken      },
                { "filter",       filter           },
                { "limit",        limit.ToString() },
                { "options",      "1"              },
                { "sign_method",  "sha256"         },
                { "timestamp",    LazadaSignatureHelper.GetTimestamp() }
            };

            parameters["sign"] = LazadaSignatureHelper.GenerateSignature(_appSecret, apiPath, parameters);

            var url = BuildUrl(_baseUrl, apiPath, parameters);
            return await SendGetAsync(url);
        }

        // Get Orders
        public async Task<JsonDocument> GetOrdersAsync(string accessToken, string status = "pending", int limit = 10, string createdAfter = null)
        {
            var apiPath = "/orders/get";

            // Default to 30 days ago if not provided
            var createdAfterDate = createdAfter ?? DateTime.UtcNow.AddDays(-30).ToString("yyyy-MM-dd HH:mm:ss");

            var parameters = new Dictionary<string, string>
            {
                { "app_key",       _appKey          },
                { "access_token",  accessToken      },
                { "status",        status           },
                { "limit",         limit.ToString() },
                { "created_after", createdAfterDate },
                { "sign_method",   "sha256"         },
                { "timestamp",     LazadaSignatureHelper.GetTimestamp() }
            };

            parameters["sign"] = LazadaSignatureHelper.GenerateSignature(_appSecret, apiPath, parameters);

            var url = BuildUrl(_baseUrl, apiPath, parameters);
            return await SendGetAsync(url);
        }

        // Get Single Order by ID
        public async Task<JsonDocument> GetOrderByIdAsync(string accessToken, string orderId)
        {
            var apiPath = "/order/get";

            var parameters = new Dictionary<string, string>
            {
                { "app_key",      _appKey      },
                { "access_token", accessToken  },
                { "order_id",     orderId      },
                { "sign_method",  "sha256"     },
                { "timestamp",    LazadaSignatureHelper.GetTimestamp() }
            };

            parameters["sign"] = LazadaSignatureHelper.GenerateSignature(_appSecret, apiPath, parameters);

            var url = BuildUrl(_baseUrl, apiPath, parameters);
            return await SendGetAsync(url);
        }

        // Get Order Items
        public async Task<JsonDocument> GetOrderItemsAsync(string accessToken, string orderId)
        {
            var apiPath = "/order/items/get";

            var parameters = new Dictionary<string, string>
            {
                { "app_key",      _appKey      },
                { "access_token", accessToken  },
                { "order_id",     orderId      },
                { "sign_method",  "sha256"     },
                { "timestamp",    LazadaSignatureHelper.GetTimestamp() }
            };

            parameters["sign"] = LazadaSignatureHelper.GenerateSignature(_appSecret, apiPath, parameters);

            var url = BuildUrl(_baseUrl, apiPath, parameters);
            return await SendGetAsync(url);
        }

        // Update Price and Stock
        public async Task<JsonDocument> UpdatePriceQuantityAsync(string accessToken, string payload)
        {
            var apiPath = "/product/price_quantity/update";

            var parameters = new Dictionary<string, string>
            {
                { "app_key",      _appKey      },
                { "access_token", accessToken  },
                { "payload",      payload      },
                { "sign_method",  "sha256"     },
                { "timestamp",    LazadaSignatureHelper.GetTimestamp() }
            };

            parameters["sign"] = LazadaSignatureHelper.GenerateSignature(_appSecret, apiPath, parameters);

            var url = BuildUrl(_baseUrl, apiPath, parameters);
            return await SendGetAsync(url);
        }

        // Helpers
        private string BuildUrl(string baseUrl, string apiPath, Dictionary<string, string> parameters)
        {
            var query = string.Join("&", parameters.Select(p => $"{p.Key}={Uri.EscapeDataString(p.Value)}"));
            return $"{baseUrl}{apiPath}?{query}";
        }

        private async Task<JsonDocument> SendGetAsync(string url)
        {
            var response = await _httpClient.GetStringAsync(url);
            return JsonDocument.Parse(response);
        }
    }
}