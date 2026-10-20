using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DomainLayer
{
    public partial class Authorization
    {
        [Key]
        public int AuthId { get; set; }

        [StringLength(20)]
        public string AuthCode { get; set; }
        [StringLength(50)]
        public string Description { get; set; }

        public bool IsActive { get; set; }

        [Column(TypeName = "date")]
        public DateTime CreateDate { get; set; }

        public int CreateUserID { get; set; }

        [Column(TypeName = "date")]
        public DateTime? UpdateDate { get; set; }

        public int? UpdateUserID { get; set; }
    }
}
