using System.Text.Json.Serialization;

namespace Application.WebApi.Models
{
    public class NetsuiteToken
    {
       
            public string access_token { get; set; }
            public int expires_in { get; set; }
            public string token_type { get; set; }
        
    }
}
