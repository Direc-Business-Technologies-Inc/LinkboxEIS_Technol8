using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DomainLayer
{
    public class EmailSetup
    {
        [Key]
        public int EmailId { get; set; }
        public string EmailCode { get; set; }
        public string EmailDesc { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public string SMTPClient { get; set; }
        public int Port { get; set; }
        public string DisplayName { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreateDate { get; set; }
        public int CreateUserID { get; set; }
        public DateTime? UpdateDate { get; set; }
        public int? UpdateUserID { get; set; }
    }
}
