using System;
using System.ComponentModel.DataAnnotations;

namespace DomainLayer
{
    public partial class Module
    {
        [Key]
        public int ModId { get; set; }

        [StringLength(20)]
        public string ModuleCode { get; set; }

        [Required]
        [StringLength(50)]
        public string ModName { get; set; }

        public bool IsActive { get; set; }

        public DateTime CreateDate { get; set; }

        public int CreateUserID { get; set; }

        public DateTime? UpdateDate { get; set; }

        public int? UpdateUserID { get; set; }
    }
}
