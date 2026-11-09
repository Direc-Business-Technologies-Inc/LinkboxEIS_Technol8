using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DomainLayer.Models
{
    public enum ExtractStatus : byte { Pending = 0, Sent = 1, Failed = 2 }
    public class EISExtractedDocument
    {
        public int Id { get; set; }
        public int MapId { get; set; }
        public string Module { get; set; }
        public int DocEntry { get; set; }
        public string EisUniqueId { get; set; }
        public string Json { get; set; }
        public ExtractStatus Status { get; set; }
        public string ErrorMessage { get; set; }
        public DateTime CreatedOn { get; set; }
        public DateTime? UpdatedOn { get; set; }
    }
}
