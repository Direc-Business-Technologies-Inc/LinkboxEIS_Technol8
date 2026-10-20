using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Security.Cryptography;
using Application.WebApi.Models.Shopee;
using Microsoft.Extensions.Configuration;

namespace Application.WebApi.Service.Shopee
{
    public class ShopeeAuthService
    {
        private readonly ShopeeClient _shopeeClient;
        private readonly IConfiguration _config;
        private static ShopeeToken _currentToken;

        private readonly string _partnerId;
        private readonly string _partnerKey;
        private readonly string _shopId;
        private readonly string _baseUrl;
        private readonly string _redirectUri;
        private readonly string _tokenFilePath;
        public string GetPartnerId() => _partnerId;
        public string GetShopId() => _shopId;

        public ShopeeAuthService(ShopeeClient shopeeClient, IConfiguration config)
        {
            _shopeeClient = shopeeClient;
            _config = config;

            // ← Shared absolute path from appsettings.json
            // Both WebAPI and LINKBOXUI point to the same file
            _tokenFilePath = Path.Combine(
                AppContext.BaseDirectory,
                "Shopee",
                "shopee_token.json"
            );

            _partnerId = config["Shopee:PartnerId"];
            _partnerKey = config["Shopee:PartnerKey"];
            _shopId = config["Shopee:ShopId"];
            _baseUrl = config["Shopee:BaseUrl"];
            _redirectUri = config["Shopee:RedirectUri"];
        }

        // -------------------------------------------------------
        // HMAC-SHA256 Signature — required on every Shopee request
        // Format: {partner_id}{path}{timestamp}
        // -------------------------------------------------------
        public string GenerateSignature(string path, long timestamp)
        {
            var rawMessage = $"{_partnerId}{path}{timestamp}";
            var keyBytes = Encoding.UTF8.GetBytes(_partnerKey);
            var msgBytes = Encoding.UTF8.GetBytes(rawMessage);

            using var hmac = new HMACSHA256(keyBytes);
            var hash = hmac.ComputeHash(msgBytes);
            return BitConverter.ToString(hash).Replace("-", "").ToLower();
        }

        // -------------------------------------------------------
        // Get current Unix timestamp
        // -------------------------------------------------------
        public long GetTimestamp() =>
            DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        // -------------------------------------------------------
        // Load token from shared JSON file
        // -------------------------------------------------------
        private void LoadTokenFromFile()
        {
            try
            {
                if (!File.Exists(_tokenFilePath))
                {
                    var dir = Path.GetDirectoryName(_tokenFilePath);
                    if (!Directory.Exists(dir))
                        Directory.CreateDirectory(dir);

                    var emptyToken = new ShopeeTokenFileModel
                    {
                        AccessToken = "",
                        RefreshToken = "",
                        AccessTokenExpiry = DateTime.UtcNow,
                        RefreshTokenExpiry = DateTime.UtcNow,
                        UpdatedDate = DateTime.UtcNow,
                        PartnerId = _partnerId,
                        ShopId = _shopId
                    };

                    var emptyJson = JsonSerializer.Serialize(emptyToken,
                        new JsonSerializerOptions { WriteIndented = true });

                    File.WriteAllText(_tokenFilePath, emptyJson);
                    Console.WriteLine($"✅ Empty Shopee token file created at: {_tokenFilePath}");
                }

                var json = File.ReadAllText(_tokenFilePath);
                var model = JsonSerializer.Deserialize<ShopeeTokenFileModel>(json);

                if (model != null)
                {
                    _currentToken = new ShopeeToken
                    {
                        AccessToken = model.AccessToken,
                        RefreshToken = model.RefreshToken,
                        AccessTokenExpiry = model.AccessTokenExpiry,
                        RefreshTokenExpiry = model.RefreshTokenExpiry,
                        PartnerId = _partnerId,
                        PartnerKey = _partnerKey,
                        ShopId = _shopId
                    };
                    Console.WriteLine("✅ Shopee token loaded from file successfully!");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading Shopee token from file: {ex.Message}");
            }
        }

        // -------------------------------------------------------
        // Save token to shared JSON file
        // -------------------------------------------------------
        private void SaveTokenToFile()
        {
            try
            {
                var model = new ShopeeTokenFileModel
                {
                    AccessToken = _currentToken.AccessToken,
                    RefreshToken = _currentToken.RefreshToken,
                    AccessTokenExpiry = _currentToken.AccessTokenExpiry,
                    RefreshTokenExpiry = _currentToken.RefreshTokenExpiry,
                    UpdatedDate = DateTime.UtcNow,
                    PartnerId = _partnerId,
                    ShopId = _shopId
                };

                var json = JsonSerializer.Serialize(model,
                    new JsonSerializerOptions { WriteIndented = true });

                var dir = Path.GetDirectoryName(_tokenFilePath);
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                File.WriteAllText(_tokenFilePath, json);
                Console.WriteLine("✅ Shopee token saved to file!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving Shopee token to file: {ex.Message}");
            }
        }

        // -------------------------------------------------------
        // Public Refresh — called by ShopeeController endpoint
        // -------------------------------------------------------
        public async Task<ShopeeToken> RefreshAndSaveTokenAsync()
        {
            LoadTokenFromFile();

            if (_currentToken?.RefreshToken == null || string.IsNullOrEmpty(_currentToken.RefreshToken))
                throw new Exception("No refresh token available! Please login first.");

            await RefreshTokenAsync();
            return _currentToken;
        }

        public async Task<ShopeeTokenFileModel> GetLastRefreshDate()
        {
            LoadTokenFromFile();
            if (_currentToken == null) return null;

            var json = File.ReadAllText(_tokenFilePath);
            var model = JsonSerializer.Deserialize<ShopeeTokenFileModel>(json);
            return model;
        }

        // -------------------------------------------------------
        // Get valid access token — mirrors Lazada exactly
        // -------------------------------------------------------
        public async Task<string> GetValidAccessTokenAsync()
        {
            // Check in-memory first
            if (_currentToken != null && DateTime.UtcNow < _currentToken.AccessTokenExpiry.AddMinutes(-5))
                return _currentToken.AccessToken;

            // Load from shared file
            LoadTokenFromFile();

            // Re-check after file load
            if (_currentToken != null && DateTime.UtcNow < _currentToken.AccessTokenExpiry.AddMinutes(-5))
                return _currentToken.AccessToken;

            // Refresh if refresh token still valid
            if (_currentToken?.RefreshToken != null &&
                !string.IsNullOrEmpty(_currentToken.RefreshToken) &&
                DateTime.UtcNow < _currentToken.RefreshTokenExpiry.AddMinutes(-5))
            {
                await RefreshTokenAsync();
                return _currentToken.AccessToken;
            }

            throw new Exception("No valid Shopee token. Please login at /api/auth/shopee/login");
        }

        // -------------------------------------------------------
        // Generate token from code (after OAuth redirect)
        // -------------------------------------------------------
        public async Task<ShopeeToken> GenerateTokenAsync(string code)
        {
            var result = await _shopeeClient.CreateAccessTokenAsync(code, _shopId);
            ParseAndStoreToken(result);
            SaveTokenToFile();
            return _currentToken;
        }

        // -------------------------------------------------------
        // Refresh token silently
        // -------------------------------------------------------
        private async Task RefreshTokenAsync()
        {
            var result = await _shopeeClient.RefreshAccessTokenAsync(
                _currentToken.RefreshToken, _shopId);
            ParseAndStoreToken(result);
            SaveTokenToFile();
        }

        // -------------------------------------------------------
        // Parse and store token in memory
        // Shopee uses "expire_in" (no 's'), default 4 hours
        // -------------------------------------------------------
        private void ParseAndStoreToken(JsonDocument doc)
        {
            var root = doc.RootElement;

            var expireIn = root.TryGetProperty("expire_in", out var expProp)
                ? expProp.GetInt32()
                : 14400;

            _currentToken = new ShopeeToken
            {
                AccessToken = root.GetProperty("access_token").GetString(),
                RefreshToken = root.GetProperty("refresh_token").GetString(),
                AccessTokenExpiry = DateTime.UtcNow.AddSeconds(expireIn),
                RefreshTokenExpiry = DateTime.UtcNow.AddDays(30),
                PartnerId = _partnerId,
                ShopId = _shopId,
                UpdatedDate = DateTime.Now
            };
        }

        // -------------------------------------------------------
        // Check if token is valid
        // -------------------------------------------------------
        public bool HasValidToken() =>
            _currentToken != null &&
            DateTime.UtcNow < _currentToken.AccessTokenExpiry.AddMinutes(-5);

        // -------------------------------------------------------
        // Get login URL — signature required for Shopee OAuth
        // -------------------------------------------------------
        public string GetLoginUrl()
        {
            var timestamp = GetTimestamp();
            var path = "/api/v2/shop/auth_partner";
            var sign = GenerateSignature(path, timestamp);

            return $"{_baseUrl}{path}?partner_id={_partnerId}" +
                   $"&timestamp={timestamp}" +
                   $"&sign={sign}" +
                   $"&redirect={Uri.EscapeDataString(_redirectUri)}";
        }
    }

    // -------------------------------------------------------
    // Token File Model — matches shopee_token.json structure
    // -------------------------------------------------------
    public class ShopeeTokenFileModel
    {
        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }
        public DateTime AccessTokenExpiry { get; set; }
        public DateTime RefreshTokenExpiry { get; set; }
        public DateTime UpdatedDate { get; set; }
        public string PartnerId { get; set; }
        public string PartnerKey { get; set; }

        public string ShopId { get; set; }
    }
}