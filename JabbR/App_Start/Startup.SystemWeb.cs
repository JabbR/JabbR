using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using JabbR.Models;
using Microsoft.AspNet.SignalR.Hubs;
using Microsoft.AspNet.SignalR.Infrastructure;
using Microsoft.AspNet.SignalR.SystemWeb.Infrastructure;
using Ninject;
using Owin;

namespace JabbR
{
    public partial class Startup
    {
        private static readonly string SystemWebHostName = "System.Web 4.5, Microsoft.Owin.Host.SystemWeb 1.0.0.0";

        // ASP.NET specific dependences (if ASP.NET isn't loaded then don't fail)
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void BindSystemWebDependencies(IKernel kernel, IAppBuilder app)
        {
            var capabilities = (IDictionary<string, object>)app.Properties["server.Capabilities"];

            // Not hosing on system web host? Bail out.
            object serverName;
            if (capabilities.TryGetValue("server.Name", out serverName) &&
                !SystemWebHostName.Equals((string)serverName, StringComparison.Ordinal))
            {
                return;
            }

            kernel.Bind<ICache>()
                  .To<AspNetCache>()
                  .InSingletonScope();

            kernel.Bind<IAssemblyLocator>()
                  .To<BuildManagerAssemblyLocator>()
                  .InSingletonScope();

            kernel.Bind<IProtectedData>()
                  .To<MachineKeyProtectedData>()
                  .InSingletonScope();
        }
    }
}