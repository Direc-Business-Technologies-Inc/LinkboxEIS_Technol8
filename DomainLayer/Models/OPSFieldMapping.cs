using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DomainLayer.Models
{
    public class OPSFieldMapping
    {
        [Key]
        public int MapId { get; set; }

        [StringLength(50)]
        public string MapCode { get; set; }

        [StringLength(50)]
        public string MapName { get; set; }
        [StringLength(50)]
        public string SAPCode { get; set; }
        [StringLength(30)]
        public string ModuleName { get; set; }
        public DateTime? CreateDate { get; set; }

        public int CreateUserID { get; set; }

        public DateTime? UpdateDate { get; set; }

        public int? UpdateUserID { get; set; }
    }
}
