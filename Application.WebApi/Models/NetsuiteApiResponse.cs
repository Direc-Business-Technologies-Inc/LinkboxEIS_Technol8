namespace Application.WebApi.Models;

public class NetsuiteApiResponse
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

        //Sales or Transfer Order Line
        public string id { get; set; }
        public string ordernumber { get; set; }
        public string status { get; set; }
        public string custitem_skubaseprice { get; set; }
        public string parent { get; set; }
        public string custitem_dbti_title { get; set; }
        public string custitem_dbti_description { get; set; }
    }

}
