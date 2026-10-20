using System;

namespace Application.WebApi.Models.Shopee
{
    public class ShopeeToken
    {
        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }
        public DateTime AccessTokenExpiry { get; set; }
        public DateTime RefreshTokenExpiry { get; set; }
        public string CountryCode { get; set; } = "PH";
        public DateTime UpdatedDate { get; set; }
        public string PartnerId { get; set; }
        public string PartnerKey { get; set; }

        public string ShopId { get; set; }
    }
}