using DomainLayer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DomainLayer.ViewModels
{
    public class ParentWithVariantsViewModel
    {
        public string ParentId { get; set; } = "";
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
        public string Sku { get; set; } = "";
        public string LastModifiedDate { get; set; } = "";  
        public string ProductId { get; set; } = "";
        public List<NetsuiteItemModel> Variants { get; set; } = new List<NetsuiteItemModel>();

    }
}
