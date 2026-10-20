using System;
using System.Collections.Generic;

namespace Application.WebApi.Models.Lazada
{
    public class LazadaOrder
    {
        public string OrderId { get; set; }
        public string Status { get; set; }
        public decimal Price { get; set; }
        public string BuyerName { get; set; }
        public string BuyerEmail { get; set; }
        public string ShippingAddress { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<LazadaOrderItem> Items { get; set; } = new List<LazadaOrderItem>();
    }

    public class LazadaOrderItem
    {
        public string OrderItemId { get; set; }
        public string ProductName { get; set; }
        public string Sku { get; set; }
        public int Quantity { get; set; }
        public decimal ItemPrice { get; set; }
    }
}