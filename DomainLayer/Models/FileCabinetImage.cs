using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DomainLayer.Models
{
    public class FileCabinetImage
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        [Required]
        [MaxLength(50)]
        public string SKUFolder { get; set; }
        [Required]
        [StringLength(10)]
        public string FileID { get; set; }
        public string Platform { get; set; }
        public string ImageID { get; set; }

        //public string Shopify { get; set; }
        //public string Zalora { get; set; }

        //public string TikTok { get; set; }

        //public string Lazada { get; set; }

        //public string Shopee { get; set; }
        public string ImageName { get; set; }
        public string Type { get; set; }

        [Column(TypeName = "varbinary(max)")]
        public byte[] ImageBinary { get; set; }
        public DateTime? CreatedDate { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public string status { get; set; }  // nvarchar(max)
    }
}
