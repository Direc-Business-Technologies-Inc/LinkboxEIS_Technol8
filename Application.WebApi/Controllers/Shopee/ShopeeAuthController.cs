using System;
using System.Threading.Tasks;
using Application.WebApi.Service.Shopee;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace Application.WebApi.Controllers.Shopee
{
    [ApiController]
    [Route("api/auth/shopee")]
    public class ShopeeAuthController : ControllerBase
    {
        private readonly ShopeeAuthService _authService;
        private readonly IConfiguration _config;

        public ShopeeAuthController(ShopeeAuthService authService, IConfiguration config)
        {
            _authService = authService;
            _config = config;
        }

        // GET /api/auth/shopee/login
        [HttpGet("login")]
        public IActionResult Login()
        {
            var loginUrl = _authService.GetLoginUrl();
            return Ok(new
            {
                message = "Copy this URL and open it in your browser to login to Shopee.",
                loginUrl = loginUrl
            });
        }

        // GET /api/auth/shopee/token?code=xxxx
        [HttpGet("token")]
        public async Task<IActionResult> GenerateToken([FromQuery] string code)
        {
            if (string.IsNullOrEmpty(code))
                return BadRequest(new
                {
                    status = "error",
                    message = "Please provide the authorization code."
                });
            try
            {
                var token = await _authService.GenerateTokenAsync(code);
                return Ok(new
                {
                    status = "success",
                    access_token = token.AccessToken,
                    refresh_token = token.RefreshToken,
                    expires_at = token.AccessTokenExpiry,
                    country = token.CountryCode,
                    partner_id = token.PartnerId,
                    shop_id = token.ShopId
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = "error", message = ex.Message });
            }
        }

        // GET /api/auth/shopee/status
        [HttpGet("status")]
        public IActionResult Status()
        {
            var hasToken = _authService.HasValidToken();
            return Ok(new
            {
                isAuthenticated = hasToken,
                message = hasToken
                    ? "Token is valid and ready."
                    : "No valid token. Please login at /api/auth/shopee/login"
            });
        }

        // GET /api/auth/shopee/config
        // ← Used by LINKBOXUI to get credentials from appsettings.json
        [HttpGet("config")]
        public IActionResult GetConfig()
        {
            return Ok(new
            {
                partner_id = _authService.GetPartnerId(),
                shop_id = _authService.GetShopId(),
                base_url = _config["Shopee:BaseUrl"],
                partner_key = _config["Shopee:PartnerKey"]
            });
        }

        // GET /api/auth/shopee/refresh
        [HttpGet("refresh")]
        public async Task<IActionResult> RefreshToken()
        {
            try
            {
                var lastRefresh = await _authService.GetLastRefreshDate();

                if (lastRefresh != null && (DateTime.UtcNow - lastRefresh.UpdatedDate).TotalHours < 1)
                {
                    return Ok(new
                    {
                        status = "success",
                        message = "Token refreshed and saved successfully!",
                        access_token = lastRefresh.AccessToken,
                        refresh_token = lastRefresh.RefreshToken,
                        access_token_expiry = lastRefresh.AccessTokenExpiry,
                        refresh_token_expiry = lastRefresh.RefreshTokenExpiry,
                        updatedate = lastRefresh.UpdatedDate,
                        partner_id = lastRefresh.PartnerId,
                        shop_id = lastRefresh.ShopId
                    });
                }

                var token = await _authService.RefreshAndSaveTokenAsync();
                return Ok(new
                {
                    status = "success",
                    message = "Token refreshed and saved successfully!",
                    access_token = token.AccessToken,
                    refresh_token = token.RefreshToken,
                    access_token_expiry = token.AccessTokenExpiry,
                    refresh_token_expiry = token.RefreshTokenExpiry,
                    updatedate = token.UpdatedDate,
                    partner_id = token.PartnerId,
                    shop_id = token.ShopId
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = "error", message = ex.Message });
            }
        }

        // GET /api/auth/shopee/debug
        [HttpGet("debug")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public IActionResult Debug()
        {
            var loginUrl = _authService.GetLoginUrl();
            return Ok(new { generatedUrl = loginUrl });
        }
    }
}