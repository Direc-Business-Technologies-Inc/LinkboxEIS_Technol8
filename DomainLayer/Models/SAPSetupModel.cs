using System;
using System.ComponentModel.DataAnnotations;

namespace DomainLayer
{
    public partial class SAPSetup
    {
        [Key]
        public int SAPId { get; set; }

        [StringLength(50)]
        public string SAPCode { get; set; }

        [StringLength(20)]
        public string SAPDBVersion { get; set; }
        public int SAPLicensePort { get; set; }
        [StringLength(50)]
        public string SAPSldServer { get; set; }

        [StringLength(50)]
        public string SAPServerName { get; set; }

        [StringLength(15)]  
        public string SAPIPAddress { get; set; }

        [StringLength(50)]
        public string SAPDBName { get; set; }

        [StringLength(10)]
        public string SAPVersion { get; set; }

        public int SAPDBPort { get; set; }

        [StringLength(20)]
        public string SAPDBuser { get; set; }

        [StringLength(50)]
        public string SAPDBPassword { get; set; }

        [StringLength(20)]
        public string SAPUser { get; set; }

        [StringLength(50)]
        public string SAPPassword { get; set; }

        public bool IsActive { get; set; }

        public DateTime? CreateDate { get; set; }

        public int CreateUserID { get; set; }

        public DateTime? UpdateDate { get; set; }

        public int? UpdateUserID { get; set; }

    }
}
