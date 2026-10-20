using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DomainLayer.Models
{
    public class Paramenter
    {
        public int Id { get; set; }
        public string Code { get; set; }
        public string ParameterType { get; set; }
        public int Value { get; set; }
        public DateTime CreateDate { get; set; }
        public DateTime? UpdateDate  { get; set; }
        public bool IsActive { get; set; }
    }
}
