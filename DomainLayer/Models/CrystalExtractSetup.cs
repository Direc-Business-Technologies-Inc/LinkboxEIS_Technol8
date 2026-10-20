using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DomainLayer.Models
{

    public class CrystalExtractSetup
    {
        [Key]
        public int Id { get; set; }

        [StringLength(50)]
        public string Name { get; set; }

        public int DocumentId { get; set; }

        [StringLength(50)]
        public string DocumentCode { get; set; }

        public int QueryId { get; set; }

        [StringLength(50)]
        public string QueryCode { get; set; }

        public int APIId { get; set; }

        [StringLength(50)]
        public string APICode { get; set; }

        public bool IsActive { get; set; }

        public DateTime? LastSync { get; set; }

        public DateTime CreateDate { get; set; }

        public int CreateUserID { get; set; }

        public DateTime? UpdateDate { get; set; }

        public int? UpdateUserID { get; set; }
    }
}
