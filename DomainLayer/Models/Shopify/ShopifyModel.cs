using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.Remoting;
using System.Text;
using System.Threading.Tasks;
using static DomainLayer.ViewModels.PostingViewModel;

namespace DomainLayer.Models.Shopify
{
    public class ShopifyModel
    {
        public List<Header> Orders { get; set; }
        
    }
    public class Header
    {
        public string id { get; set; }
        public string app_id { get; set; }
        public string order_number { get; set; }
        public string created_at { get; set; }
        public string currency { get; set; }
        public string name { get; set; }
        public string tags { get;set; }
        public string current_total_price { get; set; }
        public string current_total_discounts { get; set; }

        
        public string financial_status { get; set; }
        public string source_name { get; set; }
        public List<string> payment_gateway_names { get; set; }
        public BillingAddress billing_address { get; set; }
        public ShippingAddress shipping_address { get; set; }
        public Customer customer { get; set; }
        public List<line_item> line_items { get; set; }
        public List<discount_codes> discount_codes { get; set; } = new List<discount_codes>();
        public List<shipping_lines> shipping_lines { get; set; } = new List<shipping_lines>();


    }
    public class discount_codes
    {
        public string code { get; set; }
        public string amount { get; set; }
    }
    public class shipping_lines
    {
        public string code { get; set; }
        public string discounted_price { get; set; }
    }

    public class BillingAddress
    {
        public string first_name { get; set; }
        public string address1 { get; set; }
        public string Address2 { get; set; }
        public string city { get; set; }
        public string zip { get; set; }
        public string province { get; set; }
        public string country { get; set; }
        public string last_name { get; set; }
        public string company { get; set; }
        public string name { get; set; }
        public string country_code { get; set; }
        public string province_code { get; set; }

    }
    public class ShippingAddress
    {
        public string first_name { get; set; }
        public string address1 { get; set; }
        public string phone { get; set; }
        public string city { get; set; }
        public string zip { get; set; }
        public string province { get; set; }
        public string country { get; set; }
        public string last_name { get; set; }
        public string address2 { get; set; }
        public string company { get; set; }
        public string latitude { get; set; }
        public string longitude { get; set; }
        public string name { get; set; }
        public string country_code { get; set; }
        public string province_code { get; set; }
    }

    public class Customer
    {
        public string id { get; set; }
        public string created_at { get; set; }
        public string updated_at { get; set; }
        public string first_name { get; set; }
        public string last_name { get; set; }
        public string state { get; set; }
        public string note { get; set; }
        public string verified_email { get; set; }
        public string multipass_identifier { get; set; }
        public string tax_exempt { get; set; }
        public string tags { get; set; }
        public string email { get; set; }
        public string phone { get; set; }
        public string currency { get; set; }
    }
    public class line_item
    {
        public string id { get; set; }
        //public string admin_graphql_api_id { get; set; }
        //public List<string> attributed_staffs { get; set; }
        public string current_quantity { get; set; }
        public string fulfillable_quantity { get; set; }
        public string fulfillment_service { get; set; }
        public string fulfillment_status { get; set; }
        //public string gift_card { get; set; }
        //public string grams { get; set; }
        public string name { get; set; }
        public string price { get; set; }
        //public price_set price_set { get; set; }
        public string product_exists { get; set; }
        public string product_id { get; set; }
        //public List<string> properties { get; set; }
        public string quantity { get; set; }
        public string requires_shipping { get; set; }
        public string sku { get; set; }
        public string taxable { get; set; }
        public string title { get; set; }
        public string total_discount { get; set; }
        //public total_discount_set total_discount_set { get; set; }
        public string variant_id { get; set; }
        public string variant_inventory_management { get; set; }
        public string variant_title { get; set; }
        public string vendor { get; set; }
        //public List<string> tax_lines { get; set; }
        //public List<string> duties { get; set; }
        //public List<string> discount_allocations { get; set; }
    }

    public class price_set
    {
        public shop_money shop_money { get; set; }
        public presentment_money presentment_money { get; set; }
    }

    public class shop_money
    {
        public string amount { get; set; }
        public string currency_code { get; set; }
    }

    public class presentment_money
    {
        public string amount { get; set; }
        public string currency_code { get; set; }
    }

    public class total_discount_set
    {
        public shop_money shop_money { get; set; }
        public presentment_money presentment_money { get; set; }
    }
}
