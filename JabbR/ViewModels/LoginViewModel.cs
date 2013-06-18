using System;
using System.Collections.Generic;
using JabbR.Models;
using JabbR.Services;
using WorldDomination.Web.Authentication;

namespace JabbR.ViewModels
{
    public class LoginViewModel
    {
        public LoginViewModel(ApplicationSettings settings, IEnumerable<IAuthenticationProvider> configuredProviders, IEnumerable<ChatUserIdentity> userIdentities)
        {
            SocialDetails = new SocialLoginViewModel(configuredProviders, userIdentities);
            AllowUserRegistration = settings.AllowUserRegistration;
        }

        public bool AllowUserRegistration { get; set; }
        public SocialLoginViewModel SocialDetails { get; private set; }
    }
}