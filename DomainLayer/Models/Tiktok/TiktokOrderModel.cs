using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DomainLayer.Models.Tiktok
{
    public class TiktokOrderModel
    {
        [JsonProperty("code")]
        public int code { get; set; }

        [JsonProperty("message")]
        public string message { get; set; }

        [JsonProperty("request_id")]
        public string request_id { get; set; }

        [JsonProperty("data")]
        public TikTokOrderData data { get; set; }
    }

    public class TikTokOrderData
    {
        [JsonProperty("next_page_token")]
        public string next_page_token { get; set; }

        [JsonProperty("total_count")]
        public int total_count { get; set; }

        [JsonProperty("orders")]
        public List<TikTokOrder> orders { get; set; }
    }

    public class TikTokOrder
    {
        [JsonProperty("id")]
        public string id { get; set; }

        [JsonProperty("buyer_email")]
        public string buyer_email { get; set; }

        [JsonProperty("buyer_message")]
        public string buyer_message { get; set; }

        [JsonProperty("create_time")]
        public long create_time { get; set; }

        [JsonProperty("update_time")]
        public long update_time { get; set; }

        [JsonProperty("paid_time")]
        public long paid_time { get; set; }

        [JsonProperty("status")]
        public string status { get; set; }

        [JsonProperty("is_cod")]
        public bool is_cod { get; set; }

        [JsonProperty("tracking_number")]
        public string tracking_number { get; set; }

        [JsonProperty("shipping_provider")]
        public string shipping_provider { get; set; }

        [JsonProperty("order_type")]
        public string order_type { get; set; }

        [JsonProperty("tts_sla_time")]
        public string tts_sla_time { get; set; }

        [JsonProperty("payment_method_name")]
        public string payment_method_name { get; set; }

        [JsonProperty("line_items")]
        public List<TikTokOrderItem> line_items { get; set; }

        [JsonProperty("payment")]
        public TikTokPayment payment { get; set; }
        [JsonProperty("recipient_address")]
        public TikTokRecipientAddress recipient_address { get; set; }
    }

    public class TikTokOrderItem
    {
        [JsonProperty("id")]
        public string id { get; set; }

        [JsonProperty("product_id")]
        public string product_id { get; set; }

        [JsonProperty("product_name")]
        public string product_name { get; set; }

        [JsonProperty("seller_sku")]
        public string seller_sku { get; set; }

        [JsonProperty("sku_id")]
        public string sku_id { get; set; }

        [JsonProperty("sku_name")]
        public string sku_name { get; set; }

        [JsonProperty("sale_price")]
        public string sale_price { get; set; }

        [JsonProperty("original_price")]
        public string original_price { get; set; }

        [JsonProperty("currency")]
        public string currency { get; set; }

        [JsonProperty("tracking_number")]
        public string tracking_number { get; set; }

        [JsonProperty("package_id")]
        public string package_id { get; set; }

        [JsonProperty("is_gift")]
        public string is_gift { get; set; }

    }

    public class TikTokPayment
    {
        [JsonProperty("currency")]
        public string currency { get; set; }

        [JsonProperty("sub_total")]
        public string sub_total { get; set; }

        [JsonProperty("tax")]
        public string tax { get; set; }

        [JsonProperty("total_amount")]
        public string total_amount { get; set; }

        [JsonProperty("shipping_fee")]
        public string shipping_fee { get; set; }
        [JsonProperty("seller_discount")]
        public string seller_discount { get; set; }
        [JsonProperty("original_shipping_fee")]
        public string original_shipping_fee { get; set; }

        [JsonProperty("platform_discount")]
        public string platform_discount { get; set; }

        [JsonProperty("handling_fee")]
        public string handling_fee { get; set; }

    }
    public class TikTokRecipientAddress
    {
        [JsonProperty("address_detail")]
        public string address_detail { get; set; }

        [JsonProperty("address_line1")]
        public string address_line1 { get; set; }

        [JsonProperty("address_line2")]
        public string address_line2 { get; set; }

        [JsonProperty("address_line3")]
        public string address_line3 { get; set; }

        [JsonProperty("address_line4")]
        public string address_line4 { get; set; }

        [JsonProperty("full_address")]
        public string full_address { get; set; }

        [JsonProperty("name")]
        public string name { get; set; }

        [JsonProperty("phone_number")]
        public string phone_number { get; set; }

        [JsonProperty("postal_code")]
        public string postal_code { get; set; }

        [JsonProperty("region_code")]
        public string region_code { get; set; }


    }
}
