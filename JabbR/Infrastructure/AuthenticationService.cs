using System.Collections.Generic;
using System.Linq;
using SimpleAuthentication.Core;

namespace JabbR.Infrastructure
{
    public class AuthenticationService : IAuthenticationService
    {
        private readonly AuthenticationProviderFactory _factory;

        public AuthenticationService(AuthenticationProviderFactory factory)
        {
            _factory = factory;
        }

        public IEnumerable<IAuthenticationProvider> GetProviders()
        {
            if (_factory.AuthenticationProviders == null)
            {
                return Enumerable.Empty<IAuthenticationProvider>();
            }

            return _factory.AuthenticationProviders.Values;
        }
    }
}