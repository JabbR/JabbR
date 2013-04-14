using System;
using System.Net;
using System.Security;
using System.Text;
using System.Threading.Tasks;

using Microsoft.AspNet.SignalR.Client.Hubs;

namespace JabbR.Client
{
    public class DefaultAuthenticationProvider : IAuthenticationProvider
    {
        private readonly string _url;

        public DefaultAuthenticationProvider(string url)
        {
            _url = url;
        }

        public Task<HubConnection> Connect(string userName, string password)
        {
            var content = String.Format("username={0}&password={1}", Uri.EscapeUriString(userName), Uri.EscapeUriString(password));
            var contentBytes = Encoding.ASCII.GetBytes(content);

            var authUri = new UriBuilder(_url)
            {
                Path = "account/login"
            };

            var cookieJar = new CookieContainer();
            var request = (HttpWebRequest)WebRequest.Create(authUri.Uri);
            request.CookieContainer = cookieJar;
            request.Method = "POST";
            request.ContentType = "application/x-www-form-urlencoded";
            request.ContentLength = contentBytes.Length;

            return request.GetHttpRequestStreamAsync()
                .Then(stream => stream.WriteAsync(contentBytes).Then(() => stream.Dispose()))
                .Then(() => request.GetHttpResponseAsync())
                .Then(response => {
                    var respStatusCode = response.StatusCode;
                    
                    if (respStatusCode < HttpStatusCode.OK || respStatusCode > (HttpStatusCode)299)
                    {
                        throw new WebException(String.Format("Response status code does not indicate success: {0}", respStatusCode));
                    }
                    
                    // Verify the cookie
                    var cookie = cookieJar.GetCookies(new Uri(_url));
                    if (cookie == null || cookie["jabbr.userToken"] == null)
                    {
                        throw new SecurityException("Didn't get a cookie from JabbR! Ensure your User Name/Password are correct");
                    }
                    
                    // Create a hub connection and give it our cookie jar
                    var connection = new HubConnection(_url)
                    {
                        CookieContainer = cookieJar
                    };
                    
                    return connection;
            });
        }
    }
}
