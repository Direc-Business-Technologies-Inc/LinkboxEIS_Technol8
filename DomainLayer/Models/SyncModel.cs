using System;
using System.ComponentModel.DataAnnotations;

namespace DomainLayer
{
    public partial class Sync
    {
        [Key]
        public int Id { get; set; }

        [StringLength(100)]
        public string Code { get; set; }

        [StringLength(100)]
        public string Path { get; set; }

        [StringLength(100)]
        public string FtpPath { get; set; }

        [StringLength(30)]
        public string FtpUser { get; set; }

        [StringLength(30)]
        public string FtpPass { get; set; }

        [StringLength(100)]
        public string IpAddress { get; set; }
        [StringLength(100)]
        public string FileType { get; set; }
        [StringLength(50)]
        public string DbName { get; set; }

        [StringLength(50)]
        public string DbUser { get; set; }

        [StringLength(50)]
        public string DbVersion { get; set; }

        [StringLength(50)]
        public string DbPass { get; set; }

        public bool IsActive { get; set; }

        public DateTime CreateDate { get; set; }

        public int CreateUserID { get; set; }

        public DateTime? UpdateDate { get; set; }

        public int? UpdateUserID { get; set; }
        public string UserName { get; set; }
        public int ServerPort { get; set; }
    }
}