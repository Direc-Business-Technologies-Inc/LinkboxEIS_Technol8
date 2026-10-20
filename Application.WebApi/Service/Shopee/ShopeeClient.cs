using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace Application.WebApi.Service.Shopee
{
    public class ShopeeClient
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _config;

        private readonly string _partnerId;
        private readonly string _partnerKey;
        private readonly string _shopId;
        private readonly string _baseUrl;

        public ShopeeClient(HttpClient httpClient, IConfiguration config)
        {
            _httpClient = httpClient;
            _config = config;

            _partnerId = config["Shopee:PartnerId"];
            _partnerKey = config["Shopee:PartnerKey"];
            _shopId = config["Shopee:ShopId"];
            _baseUrl = config["Shopee:BaseUrl"];
        }

        // -------------------------------------------------------
        // Signature — HMAC-SHA256
        // Auth calls:  partner_id + path + timestamp
        // Shop calls:  partner_id + path + timestamp + access_token + shop_id
        // -------------------------------------------------------
        private string GenerateSignature(string path, long timestamp, string accessToken = null)
        {
            var raw = accessToken != null
                ? $"{_partnerId}{path}{timestamp}{accessToken}{_shopId}"
                : $"{_partnerId}{path}{timestamp}";

            var keyBytes = Encoding.UTF8.GetBytes(_partnerKey);
            var msgBytes = Encoding.UTF8.GetBytes(raw);

            using var hmac = new System.Security.Cryptography.HMACSHA256(keyBytes);
            var hash = hmac.ComputeHash(msgBytes);
            return BitConverter.ToString(hash).Replace("-", "").ToLower();
        }

        private long GetTimestamp() =>
            DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        // -------------------------------------------------------
        // Create Access Token
        // POST /api/v2/auth/token/get
        // -------------------------------------------------------
        public async Task<JsonDocument> CreateAccessTokenAsync(string oauthCode, string shopId)
        {
            var path = "/api/v2/auth/token/get";
            var timestamp = GetTimestamp();
            var sign = GenerateSignature(path, timestamp);

            var url = $"{_baseUrl}{path}?partner_id={_partnerId}&timestamp={timestamp}&sign={sign}";

            var body = new
            {
                code = oauthCode,
                shop_id = long.Parse(shopId),
                partner_id = long.Parse(_partnerId)
            };

            return await SendPostAsync(url, body);
        }

        // -------------------------------------------------------
        // Refresh Access Token
        // POST /api/v2/auth/access_token/get
        // exactly matching your friend's Postman script
        // -------------------------------------------------------
        public async Task<JsonDocument> RefreshAccessTokenAsync(string refreshToken, string shopId)
        {
            var path = "/api/v2/auth/access_token/get";
            var timestamp = GetTimestamp();
            var sign = GenerateSignature(path, timestamp);

            var url = $"{_baseUrl}{path}?partner_id={_partnerId}&timestamp={timestamp}&sign={sign}";

            var body = new
            {
                refresh_token = refreshToken,
                partner_id = long.Parse(_partnerId),
                shop_id = long.Parse(shopId)
            };

            return await SendPostAsync(url, body);
        }

        // -------------------------------------------------------
        // Get Order List
        // FIX 1: time_from = now - 15 days (matching Postman)
        // FIX 2: order_status = READY_TO_SHIP (matching Postman)
        // -------------------------------------------------------
        public async Task<JsonDocument> GetOrdersAsync(string accessToken,
            string orderStatus = "READY_TO_SHIP", int pageSize = 100, int pageNo = 1)
        {
            var path = "/api/v2/order/get_order_list";
            var timestamp = GetTimestamp();
            var sign = GenerateSignature(path, timestamp, accessToken);

            // FIX: 15 days back in Unix seconds — matches your friend's Postman
            var timeFrom = timestamp - (15 * 24 * 60 * 60);
            var timeTo = timestamp;

            var url = $"{_baseUrl}{path}" +
                      $"?partner_id={_partnerId}" +
                      $"&timestamp={timestamp}" +
                      $"&sign={sign}" +
                      $"&access_token={accessToken}" +
                      $"&shop_id={_shopId}" +
                      $"&time_range_field=create_time" +
                      $"&time_from={timeFrom}" +
                      $"&time_to={timeTo}" +
                      $"&page_size={pageSize}" +
                      $"&page_no={pageNo}" +
                      $"&order_status={orderStatus}";

            return await SendGetAsync(url);
        }

        // -------------------------------------------------------
        // Get Order Detail
        // FIX 3: simplified to match friend's Postman exactly
        // order_sn_list only — no extra optional fields
        // -------------------------------------------------------
        public async Task<JsonDocument> GetOrderDetailAsync(string accessToken, string orderSn)
        {
            var path = "/api/v2/order/get_order_detail";
            var timestamp = GetTimestamp();
            var sign = GenerateSignature(path, timestamp, accessToken);

            var url = $"{_baseUrl}{path}" +
                      $"?partner_id={_partnerId}" +
                      $"&timestamp={timestamp}" +
                      $"&sign={sign}" +
                      $"&access_token={accessToken}" +
                      $"&shop_id={_shopId}" +
                      $"&order_sn_list={orderSn}";

            return await SendGetAsync(url);
        }

        // -------------------------------------------------------
        // Helpers
        // -------------------------------------------------------
        private async Task<JsonDocument> SendPostAsync(string url, object body)
        {
            var json = JsonSerializer.Serialize(body);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(url, content);
            var result = await response.Content.ReadAsStringAsync();
            return JsonDocument.Parse(result);
        }

        private async Task<JsonDocument> SendGetAsync(string url)
        {
            var response = await _httpClient.GetStringAsync(url);
            return JsonDocument.Parse(response);
        }
    }
}