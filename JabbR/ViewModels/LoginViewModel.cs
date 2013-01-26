using System;
using System.Collections.Generic;
using JabbR.Models;
using JabbR.Services;
using WorldDomination.Web.Authentication;

namespace JabbR.ViewModels
{
    public class LoginViewModel
    {
        public LoginViewModel(AuthenticationMode authMode, IEnumerable<IAuthenticationProvider> configuredProviders, IEnumerable<ChatUserIdentity> userIdentities)
        {
            AuthenticationMode = authMode;
            SocialDetails = new SocialLoginViewModel(configuredProviders, userIdentities);
        }

        public AuthenticationMode AuthenticationMode { get; private set; }

        public SocialLoginViewModel SocialDetails { get; private set; }
    }
}