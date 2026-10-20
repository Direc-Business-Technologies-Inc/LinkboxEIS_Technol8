using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DomainLayer.Models.Shopify
{
    public class FulfillmentRequestModel
    {
        public FulfillmentRequest fulfillment { get; set; }
    }
    public class FulfillmentRequest
    {
        public List<FulfillmentLineItem> line_items_by_fulfillment_order { get; set; }
    }

    public class FulfillmentLineItem
    {
        public string fulfillment_order_id { get; set; }
    }
}
