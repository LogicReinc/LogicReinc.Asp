using LogicReinc.Asp.Arguments;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Linq;
using System.Reflection;

namespace BlazorApp.Server
{
    //ASP.Net Startup class, only used to redirect back to logic in AspServer
    internal class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            //Handled in AspServer
        }

        public void Configure(IApplicationBuilder app, IHostApplicationLifetime cycle, AspServerByPass sbps)
        {
            //Handled in AspServer
            sbps.OnConfigure(app, cycle);
        }
    }
}
