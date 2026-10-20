using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DomainLayer.Models.Shopify
{
    public class ShopifyProductModel
    {
        public Product products { get; set; }
        public Variant Variant { get; set; }
    }
    public class Product
    {
        public string id { get; set; }
        public string title { get; set; }
        public string vendor { get; set; }
        public string product_type { get; set; }
        public string status { get; set; }
        public string created_at { get; set; }


    }
    public class Variant
    {
        public string id { get; set; }
        public string product_id { get; set; }
        public string sku { get; set; } 
        public string price { get; set; }
    }
}
