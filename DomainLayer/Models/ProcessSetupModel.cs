using System;
using System.ComponentModel.DataAnnotations;

namespace DomainLayer
{
    public class ProcessSetup
    {
        [Key]
        public int ProcessId { get; set; }
        public int MapId { get; set; }
        [StringLength(50)]
        public string ProcessCode { get; set; }
        [StringLength(50)]
        public string ProcessName { get; set; }
        public string ProcessType { get; set; }

        public bool IsActive { get; set; }
        public DateTime? CreateDate { get; set; }
        public int CreateUserID { get; set; }
        public DateTime? UpdateDate { get; set; }
        public int? UpdateUserID { get; set; }
        public bool PostSAP { get; set; }
        public string APICode { get; set; }
        public string SAPCode { get; set; }
    }
}