using Newtonsoft.Json;
namespace Application.WebApi.Models
{
    public class SalesInvoiceModel
    {
        public int Entity { get; set; }
        public string TranDate { get; set; }
        public int Location { get; set; }
        public int Class { get; set; }
        public ItemDetails item { get; set; }
    }
    public class ItemDetails
    {
        public List<Item> Items { get; set; }
    }

    public class Item
    {
        public decimal Amount { get; set; }
        [JsonProperty("item")]
        public ItemInfo item { get; set; }
    }

    public class ItemInfo
    {
        public string Id { get; set; }
    }
}
