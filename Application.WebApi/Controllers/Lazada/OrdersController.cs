using System;
using System.Threading.Tasks;
using Application.WebApi.Service.Lazada;
using Microsoft.AspNetCore.Mvc;

//TESTING PURPOSES ONLY - DO NOT USE THIS FOR PRODUCTION
namespace Application.WebApi.Controllers.Lazada
{
    [ApiController]
    [Route("api/lazada/orders")]
    public class OrdersController : ControllerBase
    {
        private readonly LazadaClient _lazadaClient;
        private readonly LazadaAuthService _authService;

        public OrdersController(LazadaClient lazadaClient, LazadaAuthService authService)
        {
            _lazadaClient = lazadaClient;
            _authService = authService;
        }

        // GET /api/lazada/orders
        [HttpGet]
        public async Task<IActionResult> GetOrders(
            [FromQuery] string status = "pending",
            [FromQuery] int limit = 10,
            [FromQuery] string createdAfter = null)
        {
            try
            {

                var accessToken = await _authService.GetValidAccessTokenAsync();
                var result = await _lazadaClient.GetOrdersAsync(accessToken, status, limit, createdAfter);
                return Ok(result.RootElement);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    status = "error",
                    message = ex.Message
                });
            }
        }

        // GET /api/lazada/orders/{orderId}
        [HttpGet("{orderId}")]
        public async Task<IActionResult> GetOrderById(string orderId)
        {
            try
            {
                var accessToken = await _authService.GetValidAccessTokenAsync();
                var result = await _lazadaClient.GetOrderByIdAsync(accessToken, orderId);
                return Ok(result.RootElement);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    status = "error",
                    message = ex.Message
                });
            }
        }

        // GET /api/lazada/orders/{orderId}/items
        [HttpGet("{orderId}/items")]
        public async Task<IActionResult> GetOrderItems(string orderId)
        {
            try
            {
                var accessToken = await _authService.GetValidAccessTokenAsync();
                var result = await _lazadaClient.GetOrderItemsAsync(accessToken, orderId);
                return Ok(result.RootElement);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    status = "error",
                    message = ex.Message
                });
            }
        }
    }
}