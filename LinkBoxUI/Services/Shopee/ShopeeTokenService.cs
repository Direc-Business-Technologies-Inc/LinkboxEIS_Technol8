using Newtonsoft.Json;
using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using System.Configuration;

namespace LinkBoxUI.Services.Shopee
{
    public class ShopeeTokenService
    {
        private readonly string _tokenFilePath;
        private static string Sh_Url => ConfigurationManager.AppSettings["Sh_Url"];

        public ShopeeTokenService()
        {
            _tokenFilePath = Path.Combine(
                AppContext.BaseDirectory,
                "Shopee",
                "shopee_token.json"
            );
        }

        // -------------------------------------------------------
        // Load token from shared JSON file
        // -------------------------------------------------------
        public ShopeeTokenModel LoadToken()
        {
            try
            {
                if (!File.Exists(_tokenFilePath))
                    throw new Exception("Shopee token file not found! Please login first.");

                var json = File.ReadAllText(_tokenFilePath);
                var token = JsonConvert.DeserializeObject<ShopeeTokenModel>(json);
                return token;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error loading Shopee token: {ex.Message}");
            }
        }

        // -------------------------------------------------------
        // Save token to shared JSON file
        // -------------------------------------------------------
        public void SaveToken(ShopeeTokenModel token)
        {
            try
            {
                token.UpdatedDate = DateTime.UtcNow;
                var json = JsonConvert.SerializeObject(token, Formatting.Indented);

                var dir = Path.GetDirectoryName(_tokenFilePath);
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                File.WriteAllText(_tokenFilePath, json);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error saving Shopee token: {ex.Message}");
            }
        }

        // -------------------------------------------------------
        // Call WebAPI refresh endpoint
        // mirrors LazadaTokenService.CallRefreshEndpoint()
        // -------------------------------------------------------
        public async Task<ShopeeTokenModel> CallRefreshEndpoint()
        {
            try
            {
                using (var handler = new HttpClientHandler())
                {
                    handler.ServerCertificateCustomValidationCallback =
                        (message, cert, chain, errors) => true;

                    using (var client = new HttpClient(handler))
                    {
                        var url = $"{Sh_Url}/api/auth/shopee/refresh";
                        var response = await client.GetAsync(url);
                        var result = await response.Content.ReadAsStringAsync();

                        var refreshResponse = JsonConvert
                            .DeserializeObject<ShopeeRefreshResponseModel>(result);

                        if (refreshResponse == null)
                            throw new Exception("Failed to deserialize Shopee refresh response!");

                        var tokenModel = new ShopeeTokenModel
                        {
                            AccessToken = refreshResponse.access_token,
                            RefreshToken = refreshResponse.refresh_token,
                            AccessTokenExpiry = refreshResponse.access_token_expiry,
                            RefreshTokenExpiry = refreshResponse.refresh_token_expiry,
                            UpdatedDate = refreshResponse.updatedate,
                            PartnerId = refreshResponse.partner_id,
                            PartnerKey = refreshResponse.partnerKey,
                            ShopId = refreshResponse.shop_id
                        };

                        return tokenModel;
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error calling Shopee refresh endpoint: {ex.Message}");
            }
        }
    }

    // -------------------------------------------------------
    // Token Model
    // -------------------------------------------------------
    public class ShopeeTokenModel
    {
        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }
        public DateTime AccessTokenExpiry { get; set; }
        public DateTime RefreshTokenExpiry { get; set; }
        public DateTime UpdatedDate { get; set; }
        public string PartnerId { get; set; }
        public string ShopId { get; set; }
        public string PartnerKey { get; set; } = string.Empty;
    }

    // -------------------------------------------------------
    // Refresh Response Model
    // -------------------------------------------------------
    public class ShopeeRefreshResponseModel
    {
        public string status { get; set; }
        public string message { get; set; }
        public string access_token { get; set; }
        public string refresh_token { get; set; }
        public DateTime access_token_expiry { get; set; }
        public DateTime refresh_token_expiry { get; set; }
        public DateTime updatedate { get; set; }
        public string partner_id { get; set; }
        public string shop_id { get; set; }
        public string partnerKey { get; set; }  // ← ADD THIS
    }
}