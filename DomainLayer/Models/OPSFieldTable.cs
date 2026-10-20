using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DomainLayer.Models
{
    public class OPSFieldTable
    {
        [Key]
        [Column(Order = 1)]
        public int SAPTableId { get; set; }

        [Column(Order = 2)]
        public int MapId { get; set; }

        [Column(Order = 3)]
        public string SAPTableNameModule { get; set; }
        public string SourceTableName { get; set; }

        public string SourceColumnName { get; set; }
        public string SourceRowData { get; set; }

        public string PathCode { get; set; }

        public string FileName { get; set; }

        public string FileType { get; set; }

        public DateTime? CreateDate { get; set; }

        public int CreateUserID { get; set; }

        public DateTime? UpdateDate { get; set; }

        public int? UpdateUserID { get; set; }


    }
}
