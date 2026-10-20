using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DomainLayer.Models
{
    public class NetsuiteItemModel
    {
        public string id { get; set; }
        public string itemid { get; set; }
        public string displayname { get; set; }
        public string description { get; set; }
        public string skucode { get; set; }
        public string sku_baseprice { get; set; }
        public string createddate { get; set; }
        public string lastmodifieddate { get; set; }
        public string totalquantityonhand { get; set; }
        public string parent { get; set; }
        public string custitem_dbti_title { get; set; }
        public string custitem_dbti_description { get; set; }
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
        public List<string> parent_Uri { get; set; }
        public List<string> child_Uri { get; set; }
        public string product_id_v2 { get; set; }
        public string child_image { get; set; }
        public string shopee_id { get; set; }

        public string pkg_weight { get; set; }
        public string pkg_length { get; set; }
        public string weight_uom { get; set; }
        public string color_family { get; set; }

        public string custrecord_dbti_product_title { get; set; }




    }
}
