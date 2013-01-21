using System;
using System.Collections.Generic;
using System.Security.Principal;
using System.Threading.Tasks;
using JabbR.Infrastructure;
using JabbR.Services;
using Ninject;

namespace JabbR.Middleware
{
    using AppFunc = Func<IDictionary<string, object>, Task>;

    public class AuthorizationHandler
    {
        private readonly AppFunc _next;
        private readonly IKernel _kernel;

        public AuthorizationHandler(AppFunc next, IKernel kernel)
        {
            _next = next;
            _kernel = kernel;
        }

        public Task Invoke(IDictionary<string, object> env)
        {
            var request = new Gate.Request(env);

            var authenticationService = _kernel.Get<IAuthenticationService>();
            try
            {
                string userToken;
                string userId;
                if (request.Cookies.TryGetValue(Constants.UserTokenCookie, out userToken) &&
                    authenticationService.TryGetUserId(userToken, out userId))
                {
                    env["server.User"] = new GenericPrincipal(new GenericIdentity(userId), new string[0]);
                }
            }
            finally
            {
                authenticationService.Dispose();
            }

            return _next(env);
        }
    }
}