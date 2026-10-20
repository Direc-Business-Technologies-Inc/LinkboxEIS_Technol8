using System.Security.Cryptography.Pkcs;

namespace Application.WebApi.Models
{
    public class NetsuiteItemModel
    {

        public List<Link> links { get; set; }

        public int count { get; set; }

        public bool hasMore { get; set; }


        public List<TransactionItem> items { get; set; }

        public int offset { get; set; }

        public int totalResults { get; set; }

        public class Link
        {

            public string rel { get; set; }

            public string href { get; set; }
        }

        public class TransactionItem
        {
            public List<Link> links { get; set; }

            public string id { get; set; }
            public string itemid { get; set; }
            public string displayname { get; set; }
            public string description { get; set; }
            public string custitem_skucode { get; set; }
            public string custitem_skubaseprice { get; set; }
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
            public string pkg_weight { get; set; }
            public string pkg_length { get; set; }
            public string weight_uom { get; set; }
            public string color_family { get; set; }
            public string custrecord_dbti_product_title { get; set; }
            public string child_image { get; set; }

        }
        public class ImageItemDto
        {
            public string InternalId { get; set; }
            public string SavedFileName { get; set; }
            public string DisplayUrl { get; set; }
            public string Error { get; set; }
        }

        public class GetImagesResponse
        {
            public string Sku { get; set; }
            public List<ImageItemDto> Items { get; set; }
        }
    }
}

