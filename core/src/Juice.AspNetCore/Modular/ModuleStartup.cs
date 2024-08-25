using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Juice.Modular
{
    public abstract class ModuleStartup : IModuleStartup
    {
        /// <summary>
        /// Default value is 10
        /// </summary>
        public virtual int StartOrder => 10;

        /// <summary>
        /// Default value is StartOrder
        /// </summary>
        public virtual int ConfigureOrder => StartOrder;

        
        public virtual void Configure(IApplicationBuilder app, IEndpointRouteBuilder routes, IWebHostEnvironment env)
        {
        }

        public virtual void ConfigureServices(IServiceCollection services, IMvcBuilder mvc, IWebHostEnvironment env, IConfiguration configuration)
        {
        }

        public virtual void OnShutdown(IServiceProvider serviceProvider, IWebHostEnvironment env)
        {
        }
    }
}
