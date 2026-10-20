using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DomainLayer.Models
{
    public class SystemLog
    {
        [Key]
        public int Id { get; set; }
        public DateTime CreateDate { get; set; }
        public string Task { get; set; }
        public string ApiUrl { get; set; }
        public string Json { get; set; }
        public string ErrorMsg { get; set; }
        public string Database { get; set; }
        public string Table { get; set; }
        public string Module { get; set; }
    }
}
