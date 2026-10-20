using System;
using System.ComponentModel.DataAnnotations;

namespace DomainLayer
{
    public partial class PathSetup
    {
        [Key]
        public int PathId { get; set; }

        [StringLength(20)]
        public string PathCode { get; set; }

        [StringLength(250)]
        public string LocalPath { get; set; }

        [StringLength(250)]
        public string BackupPath { get; set; }

        [StringLength(250)]
        public string ErrorPath { get; set; }

        [StringLength(250)]
        public string RemotePath { get; set; }

        [StringLength(50)]
        public string RemoteServerName { get; set; }

        [StringLength(20)]
        public string RemoteIPAddress { get; set; }

        public int? RemotePort { get; set; }

        [StringLength(20)]
        public string RemoteUserName { get; set; }

        [StringLength(50)]
        public string RemotePassword { get; set; }

        //[StringLength(50)]
        //public string FileType { get; set; }

        public bool IsActive { get; set; }

        public DateTime? CreateDate { get; set; }

        public int CreateUserID { get; set; }

        public DateTime? UpdateDate { get; set; }

        public int? UpdateUserID { get; set; }

    }
}
