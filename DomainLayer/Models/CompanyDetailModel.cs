using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DomainLayer.Models
{
   public class CompanyDetail
    {
        public int Id { get; set; }
        public string CompanyName { get; set; }
        public string FileName { get; set; }
        public string FilePath { get; set; }
        public string Address { get; set; }
        public string MobileNo { get; set; }
        public string TelNo { get; set; }
        public DateTime CreateDate { get; set; }
        public DateTime? UpdateDate { get; set; }
        public bool IsActive { get; set; }

    }
}
