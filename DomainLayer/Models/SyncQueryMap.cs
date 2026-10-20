using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DomainLayer.Models
{
    public class SyncQueryMap
    {
        [Key]
        [Column(Order = 1)]
        [DatabaseGenerated(DatabaseGeneratedOption.None)] //property specifies that primary key values are provided by the user
        public int ReportID { get; set; }
        [Key]

        [Column(Order = 2)]
        public int QueryID { get; set; }

        public int VisOrder { get; set; }
        public int Progress { get; set; }
    }
}
