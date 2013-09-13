using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Security;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Client;

namespace JabbR.Client
{
    public class DefaultAuthenticationProvider : IAuthenticationProvider
    {
        private readonly string _url;

        public DefaultAuthenticationProvider(string url)
        {
            _url = url;
        }

        public async Task<HubConnection> Connect(string userName, string password)
        {
            var authUri = new UriBuilder(_url);
            authUri.Path += authUri.Path.EndsWith("/") ? "account/login" : "/account/login";

            var cookieJar = new CookieContainer();

#if PORTABLE
            var handler = new HttpClientHandler
            {
#else
            var handler = new WebRequestHandler
            {
#endif
                CookieContainer = cookieJar
            };

            var client = new HttpClient(handler);

            var parameters = new Dictionary<string, string> {
                { "username" , userName },
                { "password" , password }
            };

            var response = await client.PostAsync(authUri.Uri, new FormUrlEncodedContent(parameters));

            response.EnsureSuccessStatusCode();

            // Create a hub connection and give it our cookie jar
            var connection = new HubConnection(_url)
            {
                CookieContainer = cookieJar
            };

            return connection;
        }
    }
}
