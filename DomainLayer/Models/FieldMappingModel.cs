using System;
using System.ComponentModel.DataAnnotations;

namespace DomainLayer
{
    public partial class FieldMapping
    {
        [Key]
        public int MapId { get; set; }

        [StringLength(50)]
        public string MapCode { get; set; }

        [StringLength(50)]
        public string MapName { get; set; }

        [StringLength(20)]
        public string PathCode { get; set; }

        [StringLength(50)]
        public string HeaderWorksheet { get; set; }

        [StringLength(50)]
        public string RowWorksheet { get; set; }

        [StringLength(250, ErrorMessage = "The {0} value cannot exceed {1} characters.")]
        public string FileName { get; set; }

        [StringLength(20)]
        public string FileType { get; set; }

        [StringLength(50)]
        public string SAPCode { get; set; }

        [StringLength(50)]
        public string AddonCode { get; set; }

        [StringLength(30)]
        public string ModuleName { get; set; }

        [StringLength(20)]
        public string DataType { get; set; }

        public DateTime? CreateDate { get; set; }

        public int CreateUserID { get; set; }

        public DateTime? UpdateDate { get; set; }
        
        public int? UpdateUserID { get; set; }

        [StringLength(20)]
        public string APICode { get; set; }

        public DateTime? LastSync { get; set; }
        public string DestModule { get; set; }
    }
}
