using System;
using System.ComponentModel.DataAnnotations;

namespace DomainLayer
{
    public class SyncQuery
    {
        [Key]
        public int Id { get; set; }

        [StringLength(50)]
        public string SyncQueryCode { get; set; }

        public int QueryId { get; set; }

        [StringLength(50)]
        public string QueryCode { get; set; }

        public int SyncId { get; set; }

        [StringLength(50)]
        public string SyncCode { get; set; }

        public bool IsActive { get; set; }

        public DateTime? LastSync { get; set; }

        public DateTime CreateDate { get; set; }

        public int CreateUserID { get; set; }

        public DateTime? UpdateDate { get; set; }

        public int? UpdateUserID { get; set; }

    }
}