using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace LogicReinc.Asp.Arguments
{
    public interface IAspServerByPass
    {
        Action<IApplicationBuilder, IHostApplicationLifetime> OnConfigure { get; set; }
    }
    public class AspServerByPass : IAspServerByPass
    {
        public Action<IApplicationBuilder, IHostApplicationLifetime> OnConfigure { get; set; }

        public AspServerByPass(Action<IApplicationBuilder, IHostApplicationLifetime> conf)
        {
            OnConfigure = conf;
        }
    }
}
