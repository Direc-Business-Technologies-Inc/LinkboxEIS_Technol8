using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DomainLayer
{
    public class APISetup
    {
        [Key]
        public int APIId { get; set; }
        public string APICode { get; set; }

        public string APIMethod { get; set; }
        public string APIModule { get; set; }
        public string APIURL { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreateDate { get; set; }
        public int CreateUserID { get; set; }
        public DateTime? UpdateDate { get; set; }
        public int? UpdateUserID { get; set; }
        public string APIKey { get; set; }
        public string APISecretKey { get; set; }
        public string APIToken { get; set; }
        public string APILoginUrl{ get; set; }
        public string APILoginBody { get; set; }
        public string Platform { get; set; }
        public string AuthCode { get; set; }
        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }
        public string ShopCode { get; set; }

        public DateTime? AccessTokenExpiry { get; set; }
        public DateTime? RefreshTokenExpiry { get; set; }


    }
}

