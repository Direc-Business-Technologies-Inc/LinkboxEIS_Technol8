using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DomainLayer.Models
{
    public class QuerySub
    {
        [Key]
        [Column(Order = 1)]
        [DatabaseGenerated(DatabaseGeneratedOption.None)] //property specifies that primary key values are provided by the user
        public int QueryId { get; set; }

        [Key]
        [Column(Order = 2)]
        [DatabaseGenerated(DatabaseGeneratedOption.None)] //property specifies that primary key values are provided by the user
        [MaxLength(140)]
        public string code { get; set; }

        public int VisOrder {  get; set; }
        public string Query { get; set; }       
    }
}
