using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DomainLayer.Models.Zalora
{
    public class ZaloraItemModel
    {
        public List<ZaloraProductItem> items { get; set; } = new List<ZaloraProductItem>();
        
    }
    public class ZaloraProductItem
    {
        public long id { get; set; }
        
        public string name { get; set; } = string.Empty;
        public string parentSku { get; set; }
        public DateTimeOffset createdAt { get; set; }
        public DateTimeOffset updatedAt { get; set; }
        public string description { get; set; } 
        public int brandId { get; set; }
        public int primaryCategoryId { get; set; }
        public int sellerId { get; set; }

        // Dynamic/mixed attribute values (string, bool, array)
        public Dictionary<string, JToken> attributes { get; set; } = new Dictionary<string, JToken>();

       
        public string sellerSku { get; set; } = string.Empty;
        public string status { get; set; } = string.Empty;

    }
}
