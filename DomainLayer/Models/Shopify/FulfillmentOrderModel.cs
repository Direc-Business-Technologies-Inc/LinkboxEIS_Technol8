using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DomainLayer.Models.Shopify
{
    public class FulfillmentOrderModel
    {
        public List<FulfillmentOrder> fulfillment_orders { get; set; }
    }
    public class FulfillmentOrder
    {
        public string id { get; set; }
        public string status { get; set; }
    }
}
