using System.Collections.Generic;

using SimpleAuthentication.Core;

namespace JabbR.Infrastructure
{
    public interface IAuthenticationService
    {
        IEnumerable<IAuthenticationProvider> GetProviders();
    }
}