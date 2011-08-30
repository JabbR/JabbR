using System;
using System.Threading.Tasks;
using Microsoft.CSharp.RuntimeBinder;

namespace Chat {
    public class Global : System.Web.HttpApplication {

        protected void Application_Start() {
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