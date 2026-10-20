using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DomainLayer.Models
{
    public class ProcessMap
    { 
        [Key]

        [Column(Order = 1)]
        public int ProcessId { get; set; }
        [Key]

        [Column(Order = 2)]
        public int MapId { get; set; }

        public int VisOrder { get; set; }
        public int Progress { get; set; }

    }
}
