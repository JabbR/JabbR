using System;
using System.Threading.Tasks;
using Microsoft.CSharp.RuntimeBinder;
using SignalR;
using SignalR.Infrastructure;
using SignalR.Transports;

namespace Chat {
    public class Global : System.Web.HttpApplication {
        protected void Application_Start() {
            var bus = new TracedSignalBus(new InProcessSignalBus());
            DependencyResolver.Register(typeof(ISignalBus), () => bus);

            var store = new TracedMessageStore(new InProcessMessageStore());
            DependencyResolver.Register(typeof(IMessageStore), () => store);

            AppDomain.CurrentDomain.FirstChanceException += (sender, e) => {
                var ex = e.Exception.GetBaseException();
                if (!(ex is InvalidOperationException) && 
                    !(ex is RuntimeBinderException) && 
                    !(ex is MissingMethodException)) {
                    Elmah.ErrorSignal.Get(this).Raise(ex);
                }
            };

            TaskScheduler.UnobservedTaskException += (sender, e) => {
                Elmah.ErrorSignal.Get(this).Raise(e.Exception.GetBaseException());
                e.SetObserved();
            };
        }
    }
}