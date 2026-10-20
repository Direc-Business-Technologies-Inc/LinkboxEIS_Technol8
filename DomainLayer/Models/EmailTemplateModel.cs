using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace DomainLayer
{
    public class EmailTemplate
    {
        [Key]
        public int Id { get; set; }
        [StringLength(30)]
        public string Code { get; set; }
        [AllowHtml]
        [StringLength(256)]
        public string Description { get; set; }
        [StringLength(256)]
        public string Subject { get; set; }
        public string To { get; set; }
        public string CC { get; set; }
        [StringLength(30)]
        public string QueryTo { get; set; }
        [StringLength(30)]
        public string QueryCode { get; set; }
        [StringLength(30)]
        public string QueryCC { get; set; }
        [AllowHtml]
        public string Body { get; set; }
        public bool IsActive { get; set; }
        public DateTime? CreateDate { get; set; }
        public int CreateUserID { get; set; }
        public DateTime? UpdateDate { get; set; }
        public int? UpdateUserID { get; set; }
        public string CredCode { get; set; }
        public string FileCode { get; set; }
        public string Company { get; set; }
    }
}
