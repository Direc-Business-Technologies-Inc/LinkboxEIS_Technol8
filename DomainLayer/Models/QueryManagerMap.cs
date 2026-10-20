using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DomainLayer.Models
{
    public class QueryManagerMap
    {
        [Key]
        public int Id { get; set; }
        public string Field { get; set; }
        public string Condition { get; set; }
        public string Value { get; set; }
        public int QueryId { get; set; }

        public string DataType { get; set; } 
        public bool IsActive { get; set; }
        public DateTime CreateDate { get; set; }
        public int CreateUserId { get; set; }
        public DateTime? UpdateDate { get; set; }
        public int? UpdateUserId { get; set; }
    }
}
