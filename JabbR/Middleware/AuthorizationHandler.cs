using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using JabbR.Infrastructure;
using JabbR.Services;

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
                // Add the JabbR user id claim
                var claims = new List<Claim>();
                claims.Add(new Claim(ClaimTypes.NameIdentifier, userId));
                var identity = new ClaimsIdentity(claims, Constants.JabbRAuthType);

                var principal = (ClaimsPrincipal)env["server.User"];

                if (principal == null)
                {
                    principal = new ClaimsPrincipal(identity);
                }
                else
                {
                    // Add the jabbr identity to the current claims principal
                    principal.AddIdentity(identity);
                }

                env["server.User"] = principal;
            }

            return _next(env);
        }
    }
}