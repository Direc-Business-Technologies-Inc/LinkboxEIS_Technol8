using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DomainLayer.ViewModels
{
    public class BatchAllocatedViewModels
    {
        public List<BatchDetail> BatchDetails { get; set; }
        public class BatchDetail
        {
            public string BatchNum { get; set; }
            public double Quantity { get; set; }
            public string WhsCode { get; set; }
        }


    }
}
