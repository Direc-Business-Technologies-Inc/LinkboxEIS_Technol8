using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DomainLayer.Models
{
    public class APIManager
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string APICode { get; set; }
        public string Method { get; set; }
        public string QueryCode { get; set; }
        public string QueryName { get; set; }
        public string SAPCode { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreateDate { get; set; }
        public int CreateUserId { get; set; }
        public DateTime? UpdateDate { get; set; }
        public int? UpdateUserId { get; set; }
    }
}
