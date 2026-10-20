using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DomainLayer.Models
{
    public class VariantTemp
    {
        [Key]
        [Column(Order = 1)]
        [StringLength(50)]
        public string ItemCode { get; set; }
        [Key]
        [Column(Order = 2)]
        [StringLength(50)]
        public string CardCode { get; set; }
        [Key]
        [Column(Order = 3)]
        [StringLength(50)]
        public string Substitute { get; set; }
        [StringLength(50)]
        public string UomCode { get; set; }
    }
}
