using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DomainLayer.Models.Zalora
{
    public class ZaloraOrderResponse
    {
        public List<ZaloraOrderModel> Items { get; set; }
    }
    public class ZaloraOrderModel
    {
        public Guid Uuid { get; set; }
        public long Id { get; set; }
        public long SellerId { get; set; }
        public string Number { get; set; }

        public ZaloraCustomer Customer { get; set; }
        public ZaloraVoucher Voucher { get; set; }

        public string PaymentMethod { get; set; }

        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }

        public string Source { get; set; }

        // API returns {} → keep flexible
        public Dictionary<string, object> StatusList { get; set; }

        public List<long> OrderItemIds { get; set; }
        public List<ZaloraOrderItem> Items { get; set; }
    }

    public class ZaloraCustomer
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
    }

    public class ZaloraVoucher
    {
        public string Code { get; set; }
        public string Type { get; set; }
    }

    public class ZaloraOrderItem
    {
        public long Id { get; set; }
        public string SrcId { get; set; }
        public long SellerId { get; set; }
        public long OrderId { get; set; }
        public Guid Uuid { get; set; }
        public string Status { get; set; }

        public ZaloraProduct Product { get; set; }

        public decimal UnitPrice { get; set; }
        public double shippingFee { get; set; }
        public double shippingVoucherAmount { get; set; }
        public double discountAmount { get; set; }
        public double walletCredits { get; set; }

        public ZaloraPurchase Purchase { get; set; }
    }

    public class ZaloraProduct
    {
        public string Name { get; set; }
        public string Sku { get; set; }
        public string Variation { get; set; }
        public string SellerSku { get; set; }
        public string ImageUrl { get; set; }
    }

    public class ZaloraPurchase
    {
        public long? OrderSrcId { get; set; }
        public string OrderNumber { get; set; }
        public string InvoiceNumber { get; set; }
    }
}
