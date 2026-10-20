using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace DomainLayer.ViewModels
{
    public class AuthenticationCredViewModel
    {
        public string UserToken { get; set; }

        public string URL { get; set; } 

        public string Port { get; set; }

        public string Action { get; set; }

        public string Method { get; set; }
        
        public string SAPServer { get; set; } 

        public string SAPDatabase { get; set; } 

        public string SAPUserID { get; set; } 

        public string SAPPassword { get; set; } 

        public string SAPDBUserId { get; set; } 

        public string SAPDBPassword { get; set; } 

        public JObject JsonData { get; set; }

        public string JsonString { get; set; }
        public int Id { get; set; }
        public string SAPSldServer { get; set; }
    }
}
