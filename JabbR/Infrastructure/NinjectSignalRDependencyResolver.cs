using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Infrastructure;
using Microsoft.AspNet.SignalR.Messaging;
using Microsoft.AspNet.SignalR.Transports;
using Ninject;
using Ninject.Planning.Bindings;

namespace JabbR.Infrastructure
{
    internal class NinjectSignalRDependencyResolver : DefaultDependencyResolver
    {
        private readonly HashSet<Type> _azureServices = new HashSet<Type>()
        {
            typeof(IProtectedData),
            typeof(ITransportManager),
            typeof(IMessageBus),
        };

        private readonly IKernel _kernel;
        private readonly bool _usingAzureSignalR;

        public NinjectSignalRDependencyResolver(IKernel kernel, bool usingAzureSignalR)
        {
            _kernel = kernel;
            _usingAzureSignalR = usingAzureSignalR;
        }

        public override object GetService(Type serviceType)
        {
            if(_usingAzureSignalR && _azureServices.Contains(serviceType))
            {
                return base.GetService(serviceType);
            }
            return _kernel.TryGet(serviceType) ?? base.GetService(serviceType);
        }

        public override IEnumerable<object> GetServices(Type serviceType)
        {
            if(_usingAzureSignalR && _azureServices.Contains(serviceType))
            {
                return base.GetServices(serviceType);
            }
            return _kernel.GetAll(serviceType).Concat(base.GetServices(serviceType));
        }

        public override void Register(Type serviceType, Func<object> activator)
        {
            base.Register(serviceType, activator);
        }

        public override void Register(Type serviceType, IEnumerable<Func<object>> activators)
        {
            base.Register(serviceType, activators);
        }
    }
}
