using System;
using System.ComponentModel.DataAnnotations;

namespace DomainLayer
{
    public partial class User
    {
        [Key]
        public int UserId { get; set; }

        [StringLength(20)]
        public string UserName { get; set; }
        [StringLength(50)]
        public string LastName { get; set; }

        [StringLength(50)]
        public string FirstName { get; set; }

        [StringLength(50)]
        public string MiddleName { get; set; }

        [StringLength(255)]
        public string Password { get; set; }
        [StringLength(255)]
        public string UserAPIKey { get; set; }

        public bool IsActive { get; set; }

        public int? Attempt { get; set; }

        public string LastPassword { get; set; }
        
        public DateTime? LastLoginDate { get; set; }
        
        public DateTime CreateDate { get; set; }

        public int CreateUserID { get; set; }

        public DateTime? UpdateDate { get; set; }
        
        public int? UpdateUserID { get; set; }
        public int AuthorizationID { get; set; }

        public DateTime? PasswordChangedAt { get; set; }

    }
}
