using System;


namespace DomainLayer
{

    public partial class UserModule
    {
        public int Id { get; set; }
        public int ModId { get; set; }
        public int UserId { get; set; }
        public bool IsActive { get; set; }
        public DateTime? CreateDate { get; set; }
        public int CreateUserID { get; set; }
        public DateTime? UpdateDate { get; set; }
        public int? UpdateUserID { get; set; }
    }
}
