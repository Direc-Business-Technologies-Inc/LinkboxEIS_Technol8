using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DomainLayer.ViewModels
{
    public class ErrorLogsViewModel
    {
        public List<ErrorsViewModel> SystemErrorList { get; set; }

        public List<ErrorsViewModel> PrimaryErrorList { get; set; }

        public List<ErrorsViewModel> PlatformErrorList { get; set; }
        public class ErrorsViewModel
        {
            public int id { get; set; }
            public string Task { get; set; }
            public string Database { get; set; }
            public string Table { get; set; }
            public string Module { get; set; }
            public string ErrorMsg { get; set; }
            public DateTime CreateDate { get; set; }
        }
    }
}
