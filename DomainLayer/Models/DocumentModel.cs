using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DomainLayer.Models
{
    public class Document
    {
        public int Id { get; set; }
        public string Code { get; set; }
        public string FileName { get; set; }
        public string SavePath { get; set; }
        public string FilePath { get; set; }

        public int QueryManagerId { get; set; }
        public DateTime CreateDate { get; set; }
        public DateTime? UpdateDate { get; set; }
        public bool IsActive { get; set; }
        public string Credential { get; set; }
    }
}
