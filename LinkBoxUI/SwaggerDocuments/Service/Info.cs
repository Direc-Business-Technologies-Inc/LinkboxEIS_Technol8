using Microsoft.OpenApi.Models;

namespace LinkBoxUI.SwaggerDocuments.Service
{
    internal class Info : OpenApiInfo
    {
        public string Title { get; set; }
        public string Version { get; set; }
    }
}