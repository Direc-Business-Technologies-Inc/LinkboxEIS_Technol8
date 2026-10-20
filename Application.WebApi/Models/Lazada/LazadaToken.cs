using System;

namespace Application.WebApi.Models.Lazada
{
    public class LazadaToken
    {
        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }
        public DateTime AccessTokenExpiry { get; set; }
        public DateTime RefreshTokenExpiry { get; set; }
        public string CountryCode { get; set; } = "PH";
        public DateTime Updatedate { get; set; }    
        public string _appkey { get; set; }
        public string AppSecretKey { get; set; }

    }
}