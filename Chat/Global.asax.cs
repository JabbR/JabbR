using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CSharp.RuntimeBinder;
using SignalR;
using SignalR.Infrastructure;
using System.Configuration;

namespace Chat {
    public class Global : System.Web.HttpApplication {
        public static DateTimeOffset Started = DateTimeOffset.UtcNow;

        protected void Application_Start() {
            var setting = ConfigurationManager.AppSettings["traceSignals"];
            bool traceSignals;
            if (!String.IsNullOrEmpty(setting) &&
                Boolean.TryParse(setting, out traceSignals) && 
                traceSignals) {
                var bus = new TracedSignalBus(new InProcessSignalBus());
                DependencyResolver.Register(typeof(ISignalBus), () => bus);

                var store = new TracedMessageStore(new InProcessMessageStore());
                DependencyResolver.Register(typeof(IMessageStore), () => store);
            }

            AppDomain.CurrentDomain.FirstChanceException += (sender, e) => {
                var ex = e.Exception.GetBaseException();
                if (!(ex is InvalidOperationException) && 
                    !(ex is RuntimeBinderException) && 
                    !(ex is MissingMethodException) &&
                    !(ex is ThreadAbortException)) {
                    Elmah.ErrorSignal.Get(this).Raise(ex);
                }
            };

            TaskScheduler.UnobservedTaskException += (sender, e) => {
                Elmah.ErrorSignal.Get(this).Raise(e.Exception.GetBaseException());
                e.SetObserved();
            };

            Signaler.Instance.DefaultTimeout = TimeSpan.FromSeconds(25);
        }
    }
}