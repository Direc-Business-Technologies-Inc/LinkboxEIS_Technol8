
using Swashbuckle.Swagger;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Http.Description;

namespace LinkBoxUI.SwaggerDocuments
{
    public class CustomOperationFilter : IOperationFilter
    {
        public void Apply(Operation operation, SchemaRegistry schemaRegistry, ApiDescription apiDescription)
        {

            if (operation.parameters == null)
            {
                operation.parameters = new List<Parameter>();
            }

            if (apiDescription.RelativePath != null && apiDescription.RelativePath.Contains("apikey"))
            {
                operation.parameters.Add(new Parameter
                {
                    @in = "header",
                    name = "x-api-key",
                    description = "Access Api Key",
                    required = false,
                    @default = "",
                    type = "string"
                });
            }

            if (apiDescription.RelativePath != null && apiDescription.RelativePath.Contains("apitoken"))
            {
                operation.parameters.Add(new Parameter
                {
                    name = "Authorization",
                    @in = "header",
                    description = "Add you Token after Bearer. Example. Bearer r3VY0rVeF...",
                    required = false,
                    @default = "Bearer <your_token>",
                    type = "string"
                });

                ///schemaRegistry.Definitions.Add("Sales Order", SapSchema);
                ///
            }
        }

    }
}