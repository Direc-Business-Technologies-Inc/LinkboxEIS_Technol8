using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DomainLayer.Models
{
    public class OPSFieldSets
    {
        [Key]
        [Column(Order = 1)]
        public int MapId { get; set; }

        [Key]
        [Column(Order = 2)]
        public int SAPTableId { get; set; }

      

        [Key]
        [Column(Order = 3)]
        [StringLength(100)]
        public string SourceField { get; set; }


        [Key]
        [Column(Order = 4)]
        [StringLength(100)]
        public string DestinationField { get; set; }

        public string DataType { get; set; }
        public string Length { get; set; }
        public int VisOrder { get; set; }
        public bool IsKeyValue { get; set; }
        public DateTime? CreateDate { get; set; }
        public int CreateUserID { get; set; }
        public DateTime? UpdateDate { get; set; }
        public int? UpdateUserID { get; set; }
        public string SourceType { get; set; }
        public string ConditionalQuery { get; set; }
    }
}
