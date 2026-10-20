using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DomainLayer.Models
{
    public class PlatformItemTable
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

        public string ItemId { get; set; }
        public string variantId { get; set; }

        public string BuildJson { get; set; }  // nvarchar(max)

        public string PlatformJson { get; set; }  // nvarchar(max)

        public DateTime? CreatedDate { get; set; }

        public DateTime? UploadDate { get; set; }
     
        public string Message { get; set; }  // nvarchar(max)
    }
}
