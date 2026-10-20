using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DomainLayer.Models
{
    public class DocumentMap
    {
        public int Id { get; set; }

        public string CrystalParam { get; set; }

        public string QueryField { get; set; }

        public int DocId { get; set; }

        public DateTime CreateDate { get; set; }
        public int CreateUserId { get; set; }
        public DateTime? UpdateDate { get; set; }
        public int? UpdateUserId { get; set; }

    }
}
