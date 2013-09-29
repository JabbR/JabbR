using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Security.Principal;
using System.Threading.Tasks;
using JabbR.Infrastructure;
using Microsoft.Owin;

namespace JabbR.Middleware
{
    using AppFunc = Func<IDictionary<string, object>, Task>;

    public class CustomAuthHandler
    {
        private readonly AppFunc _next;

        public CustomAuthHandler(AppFunc next)
        {
            _next = next;
        }

        public async Task Invoke(IDictionary<string, object> env)
        {
            var context = new OwinContext(env);

            var claimsPrincipal = context.Request.User as ClaimsPrincipal;

            if (claimsPrincipal != null &&
                !(claimsPrincipal is WindowsPrincipal) &&
                claimsPrincipal.Identity.IsAuthenticated &&
                !claimsPrincipal.IsAuthenticated() &&
                claimsPrincipal.HasClaim(ClaimTypes.NameIdentifier))
            {
                var identity = new ClaimsIdentity(claimsPrincipal.Claims, Constants.JabbRAuthType);

                var providerName = claimsPrincipal.GetIdentityProvider();

                if (String.IsNullOrEmpty(providerName))
                {
                    // If there's no provider name just add custom as the name
                    identity.AddClaim(new Claim(ClaimTypes.AuthenticationMethod, "Custom"));
                }

                context.Authentication.SignIn(identity);
            }

            await _next(env);
        }
    }
}