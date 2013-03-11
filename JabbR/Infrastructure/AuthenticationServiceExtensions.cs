using System.Collections.Generic;
using System.Linq;
using WorldDomination.Web.Authentication;

namespace JabbR.Infrastructure
{
    public static class AuthenticationServiceExtensions
    {
        public static IEnumerable<IAuthenticationProvider> GetProviders(this IAuthenticationService authenticationService)
        {
            if (authenticationService.AuthenticationProviders == null)
            {
                return Enumerable.Empty<IAuthenticationProvider>();
            }

            return authenticationService.AuthenticationProviders.Values;
        }
    }
}