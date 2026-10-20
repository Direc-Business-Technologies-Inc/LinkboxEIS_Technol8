using System;
using System.ComponentModel.DataAnnotations;

namespace DomainLayer
{
    public partial class AuthorizationModule
    {
        [Key]
        public int Id { get; set; }
   
        public int AuthId { get; set; }
        
        public int ModId { get; set; }

        public bool IsActive { get; set; }
        
        public DateTime CreateDate { get; set; }
        
        public int CreateUserID { get; set; }

        public DateTime? UpdateDate { get; set; }
        
        public int? UpdateUserID { get; set; }

    }
}
