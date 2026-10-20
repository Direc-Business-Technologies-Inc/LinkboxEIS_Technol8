using System;
using System.Collections.Generic;
using System.Linq;
using LinkBoxUI.SwaggerDocuments.Service;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Owin;
using Owin;

[assembly: OwinStartup(typeof(LinkBoxUI.Startup))]

namespace LinkBoxUI
{
    public partial class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            //... rest of services configuration
            services.AddSwaggerDocumentation();
            //...
        }
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                //.... rest of app configuration
                app.UseSwaggerDocumentation();
            }
            //.... rest of app configuration
        }
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}
