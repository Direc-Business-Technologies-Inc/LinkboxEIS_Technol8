using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DomainLayer.ViewModels
{
    public class ApiParameterViewModel
    {
        public List<ApiParameter> ApiParameters { get; set; }
        public class ApiParameter 
        { 
            public string APICode { get; set; }
            public string APIParameter { get; set; }
            public string APIParamValue { get; set; }
        }
    }
}
