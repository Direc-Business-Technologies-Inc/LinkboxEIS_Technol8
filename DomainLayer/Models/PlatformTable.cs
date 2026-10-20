using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DomainLayer.Models
{
    [Table("PlatformTable")]
    public class PlatformTable
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string ReferenceId { get; set; }

        [Required]
        [StringLength(10)]
        public string Platform { get; set; }  // nchar(10)

        public string OrderNumber { get; set; }

        public string BuildJson { get; set; }  // nvarchar(max)

        public string PlatformJson { get; set; }  // nvarchar(max)

        [StringLength(10)]
        public string SyncStatus { get; set; }  // nchar(10)

        public string Fulfillment { get; set; }  

        public DateTime? CreatedDate { get; set; }

        public DateTime? UploadDate { get; set; }
        public DateTime? FulfilledDate { get; set; }

        public string Message { get; set; }  // nvarchar(max)

    }
}
