using System;
using System.Collections.Generic;
using System.Linq;
using JabbR.Services;
using SimpleAuthentication.Core;
using SimpleAuthentication.Core.Providers;

namespace JabbR.Infrastructure
{
    public class AuthenticationService : IAuthenticationService
    {
        private readonly AuthenticationProviderFactory _factory;

        public AuthenticationService(AuthenticationProviderFactory factory, ApplicationSettings appSettings)
        {
            _factory = factory;

            if (!String.IsNullOrWhiteSpace(appSettings.FacebookAppId) && !String.IsNullOrWhiteSpace(appSettings.FacebookAppSecret))
            {
                _factory.AddProvider(new FacebookProvider(new ProviderParams
                {
                    PublicApiKey = appSettings.FacebookAppId,
                    SecretApiKey = appSettings.FacebookAppSecret
                }));
            }
            else
            {
                _factory.RemoveProvider<FacebookProvider>();
            }
            if (!String.IsNullOrWhiteSpace(appSettings.TwitterConsumerKey) && !String.IsNullOrWhiteSpace(appSettings.TwitterConsumerSecret))
            {
                _factory.AddProvider(new TwitterProvider(new ProviderParams
                {
                    PublicApiKey = appSettings.TwitterConsumerKey,
                    SecretApiKey = appSettings.TwitterConsumerSecret
                }));
            }
            else
            {
                _factory.RemoveProvider<TwitterProvider>();
            }
            if (!String.IsNullOrWhiteSpace(appSettings.GoogleClientID) && !String.IsNullOrWhiteSpace(appSettings.GoogleClientSecret))
            {
                _factory.AddProvider(new GoogleProvider(new ProviderParams
                {
                    PublicApiKey = appSettings.GoogleClientID,
                    SecretApiKey = appSettings.GoogleClientSecret
                }));
            }
            else
            {
                _factory.RemoveProvider<GoogleProvider>();
            }
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