using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DomainLayer.ViewModels
{
    public class Link
    {
        public string Rel { get; set; }
        public string Href { get; set; }
    }

    public class Item
    {
        public string id { get; set; }
        public string ordernumber { get; set; }
        public string status { get; set; }
        public string itemid { get; set; }
        public string displayname { get; set; }
        public string description { get; set; }
        public string custitem_skucode { get; set; }
        public string custitem_skubaseprice { get; set; }
        public string createddate { get; set; }
        public string lastmodifieddate { get; set; }
        public string totalquantityonhand { get; set; }
        public string parent { get; set; }  
        public string custitem_dbti_description { get; set; }
        public string custitem_dbti_title { get; set; }
        public string product_id { get; set; }
        public string base_price { get; set; }
        public string variant_id { get; set; }
        public string custitem_suvariation { get; set; }
        public string custitem_sucategry { get; set; }
        public string custitem_subcategory { get; set; }
        public string custitem_dbti_item_display_image { get; set; }
        public string category { get; set; }
        public string catalogtype { get; set; }
        public string subcategory { get; set; }
        public string variation { get; set; }
        public string gender { get; set; }
        public string brand { get; set; }
        public string image_ids { get; set; }
        public string weight { get; set; }
        public string pkg_weight { get; set; }
        public string pkg_length { get; set; }
        public string weight_uom { get; set; }
        public string color_family { get; set; }
        public string child_image { get; set; }
        public string shopee_id { get; set; }
        public string product_id_v2 { get; set; }
        public string custrecord_dbti_product_title { get; set; }

    }

    public class Data
    {
        public List<Link> Links { get; set; }
        public int Count { get; set; }
        public bool HasMore { get; set; }
        public List<Item> Items { get; set; }
        public int Offset { get; set; }
        public int TotalResults { get; set; }
    }

    public class JsonResponseViewModel
    {
        public bool Error { get; set; }
        public string Message { get; set; }
        public Data Data { get; set; }
        public string ResponseError { get; set; }
    }
}
