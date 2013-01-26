using System;
using System.Collections.Generic;
using System.Linq;
using JabbR.Models;
using WorldDomination.Web.Authentication;

namespace JabbR.ViewModels
{
    public class SocialLoginViewModel
    {
        public SocialLoginViewModel(IEnumerable<IAuthenticationProvider> configuredProviders, IEnumerable<ChatUserIdentity> userIdentities)
        {
            ConfiguredProviders = configuredProviders != null ? configuredProviders.Select(x => x.Name) : Enumerable.Empty<string>();
            _userIdentities = userIdentities;
        }

        private readonly IEnumerable<ChatUserIdentity> _userIdentities;
        public IEnumerable<string> ConfiguredProviders { get; private set; }

        public bool IsAlreadyLinked(string providerName)
        {
            if (_userIdentities == null || !_userIdentities.Any())
            {
                return false;
            }

            return _userIdentities.Any(x => x.ProviderName.Equals(providerName, StringComparison.InvariantCultureIgnoreCase));
        }
    }
}