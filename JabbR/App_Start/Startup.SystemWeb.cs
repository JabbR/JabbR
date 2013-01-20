using System.Runtime.CompilerServices;
using JabbR.Models;
using Microsoft.AspNet.SignalR.Hubs;
using Microsoft.AspNet.SignalR.Infrastructure;
using Microsoft.AspNet.SignalR.SystemWeb.Infrastructure;
using Ninject;

namespace JabbR
{
    public partial class Startup
    {
        // ASP.NET specific dependences (if ASP.NET isn't loaded then don't fail)
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void BindSystemWebDependencies(IKernel kernel)
        {
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