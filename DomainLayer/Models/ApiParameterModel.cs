using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DomainLayer.Models
{
    public class ApiParameter
    {
        [Key]
        [Column(Order = 1)]
        public int MapId { get; set; }
        [Key]
        [Column(Order = 2)]
        public string APICode { get; set; }
        [Key]
        [Column(Order = 3)]
        public string APIParameter { get; set; }
        public string APIParamValue { get; set; }
    }
}
