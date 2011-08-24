using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Security;
using System.Web.SessionState;
using System.Threading.Tasks;

namespace Chat {
    public class Global : System.Web.HttpApplication {

        protected void Application_Start() {
            TaskScheduler.UnobservedTaskException += (sender, e) => {
                Elmah.ErrorSignal.Get(this).Raise(e.Exception.GetBaseException());
                e.SetObserved();
            };
        }
    }
}