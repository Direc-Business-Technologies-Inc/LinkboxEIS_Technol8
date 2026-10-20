using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DomainLayer.Models
{
    public partial class QueryManager
    {
        public int Id { get; set; }
        public string QueryCode { get; set; }
        public string QueryName { get; set; }
        public string ConnectionType { get; set; }
        public string ConnectionString { get; set; }

        [Column(TypeName = "text")]
        public string QueryString { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreateDate { get; set; }
        public int CreateUserId { get; set; }
        public DateTime? UpdateDate { get; set; }
        public int? UpdateUserId { get; set; }
    }
}
