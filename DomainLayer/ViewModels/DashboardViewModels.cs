using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DomainLayer.ViewModels
{
    public class DashboardViewModel
    {

        public Counter dailyCounterView { get; set; } = new Counter();

        public Counter CounterView { get; set; } = new Counter();
        public class Counter
        {
            public int ErrorCount { get; set; }
            public int Lazada { get; set; }
            public int Tiktok { get; set; }
            public int Shopee { get; set; }

        }
        public List<Item> ItemList { get; set; }
        public List<Item> ItemComparisonList { get; set; }
        public class Item
        {
            public string ItemCode { get; set; }
            public string ItemName { get; set; }
            public string Uom { get; set; }
            public string Price { get; set; }
            public string Stock { get; set; }
            public string LastUpdate { get; set; }
            public string SalesQty { get; set; }
            public string CurrentStock { get; set; }
            public string SalesPrice { get; set; }
            
        } 

        public List<DashboardReport> DashboardReportList { get; set; }
        public class DashboardReport
        {
            public string DBName { get; set; }
            public string Module { get; set; }
            public int SAPDataNo { get; set; }
            public int AddonDataNo { get; set; }
            public int IssueNo { get; set; }
        }

        public List<DashboardFieldMapping> DashboardFieldMappingList { get; set; }
        public class DashboardFieldMapping
        {
            public string MapCode { get; set; }
            public string MapName { get; set; }
            public string ModuleName { get; set; }
            public string AddonCode { get; set; }
            public string AddonDBVersion { get; set; }
            public string AddonServerName { get; set; }
            public string AddonDBuser { get; set; }
            public string AddonDBPassword { get; set; }
            public string AddonDBName { get; set; }
            public string SAPDBVersion { get; set; }
            public string SAPServerName { get; set; }
            public string SAPDBuser { get; set; }
            public string SAPDBPassword { get; set; }
            public string SAPDBName { get; set; }
        }

        public List<SAPConfig> SAPList { get; set; }
        public class SAPConfig
        { 
        public string Code { get; set; }
        }

    }
}
