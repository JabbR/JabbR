using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Threading.Tasks;
using Owin.Types;

namespace JabbR.Middleware
{    
    using AppFunc = Func<IDictionary<string, object>, Task>;

    public class AuthorizationHandler
    {
        private readonly AppFunc _next;

        public AuthorizationHandler(AppFunc next)
        {
            _next = next;
        }

        public Task Invoke(IDictionary<string, object> env)
        {
            var request = new Gate.Request(env);

            // Add new login code here

            return _next(env);
        }
    }
}