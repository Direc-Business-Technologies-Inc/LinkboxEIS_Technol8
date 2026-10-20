using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DomainLayer.Models.Shopify
{
    public class FulfillmentModel
    {
        public Fulfillment fulfillment { get; set; }
    }
    public class Fulfillment
    {
        public long id { get; set; }
        public string status { get; set; }
    }

}
