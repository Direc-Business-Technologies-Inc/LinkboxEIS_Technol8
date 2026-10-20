namespace Application.WebApi.Models.Lazada
{
    public class LazadaProduct
    {
        public string ItemId { get; set; }
        public string Name { get; set; }
        public string Sku { get; set; }
        public int Stock { get; set; }
        public decimal Price { get; set; }
        public string Status { get; set; }
    }
}