using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Owin.Types;

namespace JabbR.Auth
{
    using AppFunc = Func<IDictionary<string, object>, Task>;

    public class LoginHandler
    {
        private readonly AppFunc _next;

        public LoginHandler(AppFunc next)
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