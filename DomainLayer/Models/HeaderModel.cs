using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DomainLayer
{
    [Table("FieldSets")]
    public class Header
    {
        //[DatabaseGeneratedAttribute(DatabaseGeneratedOption.Identity), Key()]
        //[Column(Order = 0)]
        //public int Id { get; set; }
        [Key]
        [Column(Order = 1)]
        public int MapId { get; set; }
      
        [Key]
        [Column(Order = 2)]
        [StringLength(100)]
        public string SourceFieldId { get; set; }
        [Key]
        [Column(Order = 3)]
        public string SourceTableName { get; set; }
        public string SourceHeaderStart { get; set; }   
        public string SourceRowStart { get; set; }
        [Key]
        [Column(Order = 4)]
        [StringLength(100)]
        public string DestinationField { get; set; }
        [Key]
        [Column(Order = 5)]
        public string DestinationTableName { get; set; }
        public string DataType { get; set; }
        public string Length { get; set; }
        public int VisOrder { get; set; }
        public bool IsKeyValue { get; set; }
        public DateTime? CreateDate { get; set; }
        public int CreateUserID { get; set; }
        public DateTime? UpdateDate { get; set; }
        public int? UpdateUserID { get; set; }
        public string SourceType { get; set; }
        public string DefaultValue { get; set; }       
        public string ConditionalQuery { get; set; }
    }
}
