using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DomainLayer.Models
{
    public class EmailStorage
    {
        [Key]
        public int Id { get; set; }
        public string SavePath {  get; set; }
        public string FileName {  get; set; }
        public string Password { get; set; }
        public DateTime CreateDate { get; set; } = DateTime.Now;
        public DateTime? UpdateDate { get; set; }

    }
}
