using System;
using System.Threading;
using System.Threading.Tasks;
using Elmah;
using Microsoft.CSharp.RuntimeBinder;
using SignalR;

namespace Chat
{
    public class Global : System.Web.HttpApplication
    {        
        protected void Application_Start()
        {
            AppDomain.CurrentDomain.FirstChanceException += (sender, e) =>
            {
                var ex = e.Exception.GetBaseException();
                if (!(ex is InvalidOperationException) &&
                    !(ex is RuntimeBinderException) &&
                    !(ex is MissingMethodException) &&
                    !(ex is ThreadAbortException))
                {
                    ErrorSignal.Get(this).Raise(ex);
                }
            };

            TaskScheduler.UnobservedTaskException += (sender, e) =>
            {
                ErrorSignal.Get(this).Raise(e.Exception.GetBaseException());
                e.SetObserved();
            };

            Signaler.Instance.DefaultTimeout = TimeSpan.FromSeconds(25);
        }
    }
}