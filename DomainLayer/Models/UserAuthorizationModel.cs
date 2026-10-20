using System;
using System.ComponentModel.DataAnnotations;

namespace DomainLayer
{
    public partial class UserAuthorization
    {
        [Key]
        public int UserAuthId { get; set; }

        public int UserId { get; set; }
        public int AuthId { get; set; }
  
        public bool IsActive { get; set; }
        
        public DateTime CreateDate { get; set; }

        public int CreateUserID { get; set; }

        public DateTime? UpdateDate { get; set; }

        public int? UpdateUserID { get; set; }

    }
}
