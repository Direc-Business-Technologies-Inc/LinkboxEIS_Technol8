using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Application.WebApi.Models.Lazada;
using Microsoft.Extensions.Configuration;

namespace Application.WebApi.Service.Lazada
{
    public class LazadaAuthService
    {
        private readonly LazadaClient _lazadaClient;
        private readonly IConfiguration _config;
        private static LazadaToken _currentToken;

        private readonly string _appKey;
        private readonly string _appSecret;
        private readonly string _baseUrl;
        private readonly string _authUrl;
        // ← Path to the same JSON file used by LinkBoxUI
        private readonly string _tokenFilePath;

        public LazadaAuthService(LazadaClient lazadaClient, IConfiguration config)
        {
            _lazadaClient = lazadaClient;
            _config = config;

            // ← Absolute path to token file
            _tokenFilePath = Path.Combine(
                   AppContext.BaseDirectory,
                   "Lazada",
                   "lazada_token.json"
               //lazada_token.json"
               );


            _appKey = config["Lazada:AppKey"];
            _appSecret = config["Lazada:AppSecret"];
            _baseUrl = config["Lazada:BaseUrl"];
            _authUrl = config["Lazada:AuthUrl"];
        }

        // -------------------------------------------------------
        // Load token from JSON file on startup
        // -------------------------------------------------------
        private async Task LoadTokenFromFile()
        {
            try
            {
                if (!File.Exists(_tokenFilePath))
                {
                    var dir = Path.GetDirectoryName(_tokenFilePath);
                    if (!Directory.Exists(dir))
                        Directory.CreateDirectory(dir);

                    // ← define emptyToken first
                    var emptyToken = new LazadaTokenFileModel
                    {
                        AccessToken = "",
                        RefreshToken = "",
                        AccessTokenExpiry = DateTime.UtcNow,
                        RefreshTokenExpiry = DateTime.UtcNow,
                        UpdatedDate = DateTime.UtcNow,
                        _appkey = _appKey,
                        appSecretKey = _appSecret

                    };

                    var emptyJson = System.Text.Json.JsonSerializer.Serialize(emptyToken,
                        new JsonSerializerOptions { WriteIndented = true });

                    File.WriteAllText(_tokenFilePath, emptyJson);
                    Console.WriteLine($"✅ Empty token file created at: {_tokenFilePath}");
                }

                var json = File.ReadAllText(_tokenFilePath);
                var model = System.Text.Json.JsonSerializer.Deserialize<LazadaTokenFileModel>(json);

                if (model != null)
                {
                    _currentToken = new LazadaToken
                    {
                        AccessToken = model.AccessToken,
                        RefreshToken = model.RefreshToken,
                        AccessTokenExpiry = model.AccessTokenExpiry,
                        RefreshTokenExpiry = model.RefreshTokenExpiry,
                        CountryCode = "PH",
                        _appkey = _appKey,
                        AppSecretKey = _appSecret

                    };
                    Console.WriteLine("✅ Lazada token loaded from file successfully!");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading token from file: {ex.Message}");
            }
        }

        // -------------------------------------------------------
        // Save token back to JSON file after refresh
        // -------------------------------------------------------
        private void SaveTokenToFile()
        {
            try
            {
                var model = new LazadaTokenFileModel
                {
                    AccessToken = _currentToken.AccessToken,
                    RefreshToken = _currentToken.RefreshToken,
                    AccessTokenExpiry = _currentToken.AccessTokenExpiry,
                    RefreshTokenExpiry = _currentToken.RefreshTokenExpiry,
                    UpdatedDate = DateTime.UtcNow,
                    appSecretKey = _appSecret,
                    _appkey = _appKey

                };

                var json = System.Text.Json.JsonSerializer.Serialize(model,
                    new JsonSerializerOptions { WriteIndented = true });

                var dir = Path.GetDirectoryName(_tokenFilePath);
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                File.WriteAllText(_tokenFilePath, json);
                Console.WriteLine("✅ Lazada token saved to file!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving token to file: {ex.Message}");
            }
        }
        // -------------------------------------------------------
        // Public Refresh — called by AuthController endpoint
        // -------------------------------------------------------
        public async Task<LazadaToken> RefreshAndSaveTokenAsync()
        {
            // Try loading from file first in case it was updated elsewhere
            await LoadTokenFromFile();

            if (_currentToken?.RefreshToken == null || string.IsNullOrEmpty(_currentToken.RefreshToken))
                throw new Exception("No refresh token available! Please login first.");

            // Always refresh and save to file
            await RefreshTokenAsync();
            return _currentToken;
        }

        public async Task<LazadaTokenFileModel> GetLastRefreshDate()
        {
            await LoadTokenFromFile();
            if (_currentToken == null) return null;

            // Read UpdatedDate from file
            var json = File.ReadAllText(_tokenFilePath);
            var model = System.Text.Json.JsonSerializer.Deserialize<LazadaTokenFileModel>(json);
            return model;
        }

        // -------------------------------------------------------
        // Get valid access token — loads from file if needed
        // -------------------------------------------------------
        public async Task<string> GetValidAccessTokenAsync()
        {
            // If in-memory token is valid, return it
            if (_currentToken != null && DateTime.UtcNow < _currentToken.AccessTokenExpiry.AddMinutes(-5))
                return _currentToken.AccessToken;

            // Try loading from file again in case it was updated
            await LoadTokenFromFile();

            // Check again after loading from file
            if (_currentToken != null && DateTime.UtcNow < _currentToken.AccessTokenExpiry.AddMinutes(-5))
                return _currentToken.AccessToken;

            // Try refresh token
            if (_currentToken?.RefreshToken != null && DateTime.UtcNow < _currentToken.RefreshTokenExpiry.AddMinutes(-5))
            {
                await RefreshTokenAsync();
                return _currentToken.AccessToken;
            }

            throw new Exception("No valid token. Please login at /api/auth/lazada/login");
        }

        // -------------------------------------------------------
        // Generate token from code and return it
        // -------------------------------------------------------
        public async Task<LazadaToken> GenerateTokenAsync(string code)
        {
            var result = await _lazadaClient.CreateAccessTokenAsync(code);
            ParseAndStoreToken(result);
            SaveTokenToFile(); // ← save after login too
            return _currentToken;
        }

        // -------------------------------------------------------
        // Exchange code — used internally
        // -------------------------------------------------------
        public async Task ExchangeCodeForTokenAsync(string code)
        {
            var result = await _lazadaClient.CreateAccessTokenAsync(code);
            ParseAndStoreToken(result);
            SaveTokenToFile();
        }

        // -------------------------------------------------------
        // Refresh token silently
        // -------------------------------------------------------
        private async Task RefreshTokenAsync()
        {
            var result = await _lazadaClient.RefreshAccessTokenAsync(_currentToken.RefreshToken);
            ParseAndStoreToken(result);
            SaveTokenToFile(); // ← save after refresh
        }

        // -------------------------------------------------------
        // Parse and store token in memory
        // -------------------------------------------------------
        private void ParseAndStoreToken(JsonDocument doc)
        {
            var root = doc.RootElement;
            _currentToken = new LazadaToken
            {
                AccessToken = root.GetProperty("access_token").GetString(),
                RefreshToken = root.GetProperty("refresh_token").GetString(),
                AccessTokenExpiry = DateTime.UtcNow.AddSeconds(root.GetProperty("expires_in").GetInt32()),
                RefreshTokenExpiry = DateTime.UtcNow.AddDays(30),
                CountryCode = "PH",
                _appkey = _appKey,
                AppSecretKey = _appSecret,
                Updatedate = DateTime.Now
                

            };
        }

        // -------------------------------------------------------
        // Check if token exists and valid
        // -------------------------------------------------------
        public bool HasValidToken()
        {
            return _currentToken != null && DateTime.UtcNow < _currentToken.AccessTokenExpiry.AddMinutes(-5);
        }

        // -------------------------------------------------------
        // Get login URL
        // -------------------------------------------------------
        public string GetLoginUrl()
        {
            var appKey = _config["Lazada:AppKey"];
            var redirectUri = _config["Lazada:RedirectUri"];
            return $"https://auth.lazada.com/oauth/authorize?response_type=code&force_auth=true&redirect_uri={Uri.EscapeDataString(redirectUri)}&client_id={appKey}";
        }
    }

    // -------------------------------------------------------
    // Token File Model — matches lazada_token.json structure
    // -------------------------------------------------------
    public class LazadaTokenFileModel
    {
        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }
        public DateTime AccessTokenExpiry { get; set; }
        public DateTime RefreshTokenExpiry { get; set; }
        public DateTime UpdatedDate { get; set; }
        public string _appkey { get; set; }
        public string appSecretKey { get; set; }
    }
}