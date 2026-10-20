using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DomainLayer
{
    public partial class Query
    {
        [Key]
        public int Id { get; set; }

        [StringLength(100)]
        public string Code { get; set; }

        [Column(TypeName = "text")]
        public string QueryString { get; set; }
        public bool IsActive { get; set; }

        public DateTime CreateDate { get; set; }

        public int CreateUserID { get; set; }

        public DateTime? UpdateDate { get; set; }

        public int? UpdateUserID { get; set; }
        public bool IsMultiQuery { get; set; } = false;
    }
}