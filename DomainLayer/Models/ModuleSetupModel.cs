using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DomainLayer.Models
{
    public class ModuleSetup
    {
        [Key]
        public int Id { get; set; }
        [MaxLength(25)]
        public string ModuleCode { get; set; }
        public string ModuleName { get; set; }
        public string PrimaryKey { get; set; }
        public bool IsActive { get; set; }
        public DateTime? CreateDate { get; set; }
        public int CreateUserID { get; set; }
        public DateTime? UpdateDate { get; set; }
        public int? UpdateUserID { get; set; }
        public string EntityType { get; set; }
        public string EntityName { get; set; }
    }
}
