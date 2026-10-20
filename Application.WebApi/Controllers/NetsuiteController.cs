using Application.WebApi.Models;
using Application.WebApi.Service;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Text.Json;

namespace Application.WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class NetsuiteController : ControllerBase
    {
        //NetsuiteService netsuiteservice = new NetsuiteService();

        private readonly NetsuiteService netsuiteservice;

        // Inject NetsuiteService through the constructor
        public NetsuiteController(NetsuiteService _netsuiteService)
        {
            netsuiteservice = _netsuiteService;
        }

        [HttpGet("PostData")]
        public IActionResult PostDataGet()
        {
            return Ok("GET allowed");
        }

        [HttpPost("PostData")]
        public async Task<IActionResult> NetsuitePost([FromBody] JsonElement request)
        {
            string Data = request.GetProperty("query").GetString();
            string module = request.GetProperty("module").GetString();
            string method = request.GetProperty("method").GetString() ?? "POST";
            string id = request.GetProperty("id").GetString() ?? "";
            if (string.IsNullOrEmpty(Data))
            {
                return BadRequest("error: Invalid request body: Data is null or empty.");
            }

            try
            {
                var (isSuccess, responseMessage) = await netsuiteservice.NetsuitePostData(Data,module,method,id);

                if (isSuccess)
                {
                    return Ok(new { message = $" {module.Replace("customtransaction_gvi_wthdrwal","withdrawal")} successfully posted.", response = responseMessage });
                }
                else
                {
                    return StatusCode(500, new { error = $"Failed to post {module}", details = responseMessage });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Internal server error.", details = ex.Message });
            }
        }


        [HttpGet("GetAccessToken")]
        public async Task<IActionResult> GetAccessToken()
        {
            try
            {
                string response = await netsuiteservice.GetAccessToken();

                if (!response.Contains("Error"))
                {
                    return Ok(response);
                }
                else
                {
                    return StatusCode(500, response);
                }
            }
            catch (Exception ex)
            {
                // Log the exception for further investigation if necessary
                return StatusCode(500, ex.Message);
            }
        }

        [HttpPost("QueryData")]
        public async Task<IActionResult> QueryService([FromBody] string query)
        {
            try
            {
                // Validate the query input
                if (string.IsNullOrWhiteSpace(query))
                {
                    return BadRequest(new Response
                    {
                        Error = true,
                        Message = "QueryBody cannot be null or empty."
                    });
                }

                // Call the QueryService and await its result
                var result = await netsuiteservice.QueryService(query);

                // Ensure the result is not null
                if (result == null)
                {
                    return StatusCode(500, new Response
                    {
                        Error = true,
                        Message = "error:No response received from the QueryService."
                    });
                }

                // Check if the result indicates an error
                if (result.Error)
                {
                    return StatusCode(500, new Response
                    {
                        Error = true,
                        Message = result.Message,
                        ResponseError = result.ResponseError
                    });
                }

                // Return a successful result
                return Ok(new Response
                {
                    Error = false,
                    Message = "Query executed successfully.",
                    Data = result.Data
                });
            }
            catch (Exception ex)
            {
                // Log the exception for debugging
                Console.Error.WriteLine(ex);

                // Return a generic error response
                return StatusCode(500, new Response
                {
                    Error = true,
                    Message = "An unexpected error occurred.",
                    ResponseError = ex.ToString()
                });
            }
        }

        [HttpPost("ItemQueryData")]
        public async Task<IActionResult> ItemQueryService([FromBody] string query)
        {   
            try
            {
                // Validate the query input
                if (string.IsNullOrWhiteSpace(query))
                {
                    return BadRequest(new Response
                    {
                        Error = true,
                        Message = "QueryBody cannot be null or empty."
                    });
                }

                // Call the QueryService and await its result
                var result = await netsuiteservice.ItemQueryService(query);

                // Ensure the result is not null
                if (result == null)
                {
                    return StatusCode(500, new Response
                    {
                        Error = true,
                        Message = "error:No response received from the QueryService."
                    });
                }

                // Check if the result indicates an error
                if (result.Error)
                {
                    return StatusCode(500, new Response
                    {
                        Error = true,
                        Message = result.Message,
                        ResponseError = result.ResponseError
                    });
                }

                // Return a successful result
                return Ok(new Response
                {
                    Error = false,
                    Message = "Query executed successfully.",
                    Data = result.Data
                });
            }
            catch (Exception ex)
            {
                // Log the exception for debugging
                Console.Error.WriteLine(ex);

                // Return a generic error response
                return StatusCode(500, new Response
                {
                    Error = true,
                    Message = "An unexpected error occurred.",
                    ResponseError = ex.ToString()
                });
            }
        }



        [HttpPost("NetsuiteColumnData")]
        public async Task<IActionResult> ColumnQueryService([FromBody] string query)
        {
            try
            {
                // Validate the query input
                if (string.IsNullOrWhiteSpace(query))
                {
                    return BadRequest(new Response
                    {
                        Error = true,
                        Message = "QueryBody cannot be null or empty."
                    });
                }

                // Call the QueryService and await its result
                var result = await netsuiteservice.ColumnsQueryService(query);

                // Ensure the result is not null
                if (result == null)
                {
                    return StatusCode(500, new Response
                    {
                        Error = true,
                        Message = "error:No response received from the QueryService."
                    });
                }

                // Check if the result indicates an error
                if (result.Error)
                {
                    return StatusCode(500, new Response
                    {
                        Error = true,
                        Message = result.Message,
                        ResponseError = result.ResponseError
                    });
                }

                // Return a successful result
                return Ok(new Response
                {
                    Error = false,
                    Message = "Query executed successfully.",
                    Data = result.Data
                });
            }
            catch (Exception ex)
            {
                // Log the exception for debugging
                Console.Error.WriteLine(ex);

                // Return a generic error response
                return StatusCode(500, new Response
                {
                    Error = true,
                    Message = "An unexpected error occurred.",
                    ResponseError = ex.ToString()
                });
            }
        }


        [HttpPost("GetNetsuiteColumn")]
        public async Task<IActionResult> GetNetsuiteColumn([FromBody] JsonElement request)
        {
            try
            {
                // Validate the query input
                string module = request.GetProperty("module").GetString();
                string id = request.GetProperty("id").GetString();
                string expand = request.GetProperty("expand").GetString();


                // Call the QueryService and await its result
                var result = await netsuiteservice.GetColumnsNetsuite(module,id,expand);

                // Ensure the result is not null
                if (result == null)
                {
                    return StatusCode(500, new Response
                    {
                        Error = true,
                        Message = "error:No response received"
                    });
                }

                // Check if the result indicates an error
                if (result.Error)
                {
                    return StatusCode(500, new Response
                    {
                        Error = true,
                        Message = result.Message,
                        ResponseError = result.ResponseError
                    });
                }

                // Return a successful result
                return Ok(new Response
                {
                    Error = false,
                    Message = " executed successfully.",
                    Data = result.Data
                });
            }
            catch (Exception ex)
            {
                // Log the exception for debugging
                Console.Error.WriteLine(ex);

                // Return a generic error response
                return StatusCode(500, new Response
                {
                    Error = true,
                    Message = "An unexpected error occurred.",
                    ResponseError = ex.ToString()
                });
            }
        }
    }
}
