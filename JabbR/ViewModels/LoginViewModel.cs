using System;
using System.Collections.Generic;
using JabbR.Models;
using JabbR.Services;
using WorldDomination.Web.Authentication;

namespace JabbR.ViewModels
{
    public class LoginViewModel
    {
        public LoginViewModel(IEnumerable<IAuthenticationProvider> configuredProviders, IEnumerable<ChatUserIdentity> userIdentities)
        {
            SocialDetails = new SocialLoginViewModel(configuredProviders, userIdentities);
        }

        public SocialLoginViewModel SocialDetails { get; private set; }
    }
}