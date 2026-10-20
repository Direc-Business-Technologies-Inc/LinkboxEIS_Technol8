using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DomainLayer.ViewModels
{
    public class ExcelMapperViewModel
    {
        public List<ExcelMapperModel> ExcelMapList { get; set; }
        public class ExcelMapperModel
        {
            public int MapId { get; set; }
            public int SAPTableId { get; set; }
            public string Worksheet { get; set; }
            public string HeaderRow { get; set; }            
            public string DataRowStart { get; set; }
            public string SourceType { get; set; }
            public string DestinationTable { get; set; }
            public string EntityName { get; set; }
            public List<ExcelMapping> Mapping { get; set; } = new List<ExcelMapping>();
        }

        public class ExcelMapping
        {
            public int id { get; set; }
            public string Source { get; set; }
            public string Target { get; set; }
            public bool IsSingleCell { get; set; } = false;
        }
    }
}
