using System;
using System.ComponentModel.DataAnnotations;

namespace DomainLayer
{

    public partial class AddonSetup
    {
        [Key]
        public int AddonId { get; set; }

        [StringLength(50)]
        public string AddonCode { get; set; }

        [StringLength(20)]
        public string AddonDBVersion { get; set; }

        [StringLength(50)]
        public string AddonServerName { get; set; }

        [StringLength(20)]
        public string AddonIPAddress { get; set; }

        [StringLength(50)]
        public string AddonDBName { get; set; }

        public int? AddonPort { get; set; }

        [StringLength(20)]  
        public string AddonDBuser { get; set; }

        [StringLength(50)]
        public string AddonDBPassword { get; set; }

        public bool IsActive { get; set; }

        public DateTime? CreateDate { get; set; }
       
        public int CreateUserID { get; set; }

        public DateTime? UpdateDate { get; set; }
        
        public int? UpdateUserID { get; set; }

    }
}
