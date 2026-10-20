using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace LinkBoxUI.SwaggerDocuments
{
    public class SapSchema 
    {
        public string CardCode { get; set; }
        public string CardName { get; set; }
        public List<DocumentLines> Documents { get; set; }
        public class DocumentLines
        {
            public string ItemCode { get; set; }
            public string ItemName { get; set; }
            public int LineNum { get; set; }
            public double Quantity { get; set; }

        }
    }
}