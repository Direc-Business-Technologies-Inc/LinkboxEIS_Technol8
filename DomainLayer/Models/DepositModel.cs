using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DomainLayer
{
    public partial class Deposit
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int BrkId { get; set; }

        [StringLength(20)]
        public string BrkCode { get; set; }

        [StringLength(20)]
        public string BrkDescription { get; set; }
        public bool IsActive { get; set; }
        
        public DateTime? CreateDate { get; set; }
        
        public int CreateUserID { get; set; }

        public DateTime? UpdateDate { get; set; }
        
        public int? UpdateUserID { get; set; }

    }
}
