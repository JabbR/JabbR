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
        private readonly IAuthenticationTokenService _authenticationTokenService;

        public AuthorizationHandler(AppFunc next, IAuthenticationTokenService authenticationTokenService)
        {
            _next = next;
            _authenticationTokenService = authenticationTokenService;
        }

        public Task Invoke(IDictionary<string, object> env)
        {
            var request = new Gate.Request(env);

            string userToken;
            string userId;
            if (request.Cookies.TryGetValue(Constants.UserTokenCookie, out userToken) &&
                _authenticationTokenService.TryGetUserId(userToken, out userId))
            {
                env["server.User"] = new GenericPrincipal(new GenericIdentity(userId), new string[0]);
            }
            else
            {
                env["windows.User"] = env["server.User"] as WindowsPrincipal;
                env["server.User"] = null;
            }

            return _next(env);
        }
    }
}