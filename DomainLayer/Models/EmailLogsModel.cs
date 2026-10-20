using System;
using System.ComponentModel.DataAnnotations;
using System.Data.Entity.Validation;

namespace DomainLayer
{

    public partial class EmailLogs
    {
        [Key]
        public int EmailId { get; set; }

        
        [StringLength(30)]
        public string EmailCode { get; set; }

        [StringLength(30)]
        public string EmailName { get; set; }

        [StringLength(250)]
        public string EmailFrom { get; set; }

        [StringLength(20)]
        public string EmailPassword { get; set; }

        [StringLength(250)]
        public string EmailSubject { get; set; }

        [StringLength(50)]
        public string EmailHost { get; set; }

        public int EmailPort { get; set; }

        [StringLength(10)]
        public string EmailSsl { get; set; }

        [StringLength(250)]
        public string EmailTo { get; set; }

        [StringLength(255)]
        public string EmailCc { get; set; }

        [StringLength(50)]
        public string EmailLog { get; set; }

        public int AddonId { get; set; }

        public bool IsActive { get; set; }

        public DateTime? CreateDate { get; set; }

        public int CreateUserID { get; set; }

        public DateTime? UpdateDate { get; set; }

        public int? UpdateUserID { get; set; }
        
    }
}
