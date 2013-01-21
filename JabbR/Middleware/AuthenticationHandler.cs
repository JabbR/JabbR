using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Owin.Types;

namespace JabbR.Middleware
{
    using AppFunc = Func<IDictionary<string, object>, Task>;

    public class AuthenticationHandler
    {
        private readonly AppFunc _next;

        public AuthenticationHandler(AppFunc next)
        {
            _next = next;
        }

        public Task Invoke(IDictionary<string, object> env)
        {
            var request = new OwinRequest(env);
            var response = new OwinResponse(env);

            // Add new login code here

            return _next(env);
        }
    }
}