using System;
using System.Threading.Tasks;
using Application.WebApi.Service.Lazada;
using Microsoft.AspNetCore.Mvc;

namespace Application.WebApi.Controllers.Lazada
{
    [ApiController]
    [Route("api/auth/lazada")]
    public class AuthController : ControllerBase
    {
        private readonly LazadaAuthService _authService;

        public AuthController(LazadaAuthService authService)
        {
            _authService = authService;
        }

        // GET /api/auth/lazada/login
        [HttpGet("login")]
        public IActionResult Login()
        {
            var loginUrl = _authService.GetLoginUrl();
            return Ok(new
            {
                message = "Copy this URL and open it in your browser to login to Lazada.",
                loginUrl = loginUrl
            });
        }

        // GET /api/auth/lazada/token?code=xxxx
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
                    app_key = token._appkey,
                    secret_key = token.AppSecretKey

                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = "error", message = ex.Message });
            }
        }

        // GET /api/auth/lazada/status
        [HttpGet("status")]
        public IActionResult Status()
        {
            var hasToken = _authService.HasValidToken();
            return Ok(new
            {
                isAuthenticated = hasToken,
                message = hasToken
                    ? "Token is valid and ready."
                    : "No valid token. Please login at /api/auth/lazada/login"
            });
        }

        // ← NEW: Refresh token endpoint
        // GET /api/auth/lazada/refresh
        [HttpGet("refresh")]
        public async Task<IActionResult> RefreshToken()
        {
            try
            {
                //    var lastRefresh = await  _authService.GetLastRefreshDate();

                //    if (lastRefresh != null &&
                //        DateTime.UtcNow.Date >= lastRefresh.UpdatedDate.Date.AddDays(7))
                //    {
                //        return Ok(new
                //        {
                //            status = "success",
                //            message = "Token refreshed and saved successfully!",
                //            access_token = lastRefresh.AccessToken,
                //            refresh_token = lastRefresh.RefreshToken,
                //            access_token_expiry = lastRefresh.AccessTokenExpiry,
                //            refresh_token_expiry = lastRefresh.RefreshTokenExpiry,
                //            updatedate = lastRefresh.UpdatedDate,
                //            app_key = lastRefresh._appkey,
                //            secret_key = lastRefresh.appSecretKey
                //        });
                //    }
                var token = await _authService.RefreshAndSaveTokenAsync();
                //RefreshTokenAsync
                return Ok(new
                {
                    status = "success",
                    message = "Token refreshed and saved successfully!",
                    access_token = token.AccessToken,
                    refresh_token = token.RefreshToken,
                    access_token_expiry = token.AccessTokenExpiry,
                    refresh_token_expiry = token.RefreshTokenExpiry,
                    UpdateDate = token.Updatedate,
                    app_key = token._appkey,
                    secret_key = token.AppSecretKey

                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = "error", message = ex.Message });
            }
        }

        // GET /api/auth/lazada/debug
        [HttpGet("debug")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public IActionResult Debug()
        {
            var loginUrl = _authService.GetLoginUrl();
            return Ok(new { generatedUrl = loginUrl });
        }
    }
}