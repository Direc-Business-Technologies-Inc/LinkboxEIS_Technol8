using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DomainLayer
{
    public partial class UploadSchedule
    {
        [Key]
        public int SchedId { get; set; }

        [StringLength(20)]
        public string SchedCode { get; set; }

        public string Process { get; set; }

        [StringLength(20)]
        public string Frequency { get; set; }

        [Column(TypeName = "date")]
        public DateTime? StartDate { get; set; }

        public DateTime? StartTime { get; set; }

        public string ScheduleType { get; set; }
        public bool IsActive { get; set; }

        public DateTime? CreateDate { get; set; }

        public int CreateUserID { get; set; }

        public DateTime? UpdateDate { get; set; }
        public int? UpdateUserID { get; set; }
        public string Api { get; set; }
        public string Credential { get; set; }
        public bool IsRunning { get; set; }
        public DateTime?RunTime { get; set; }
        public bool ForceStop { get; set; }
    }
}
