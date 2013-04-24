using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Cache;
using System.Security.Principal;
using System.Threading.Tasks;
using JabbR.ContentProviders;
using JabbR.Infrastructure;
using JabbR.Services;
using Owin.Types;
using Owin.Types.Extensions;

namespace JabbR.Middleware
{
    using AppFunc = Func<IDictionary<string, object>, Task>;

    /// <summary>
    /// Proxies images through the jabbr server to avoid mixed mode https.
    /// </summary>
    public class ImageProxyHandler
    {
        private readonly AppFunc _next;
        private readonly IJabbrConfiguration _configuration;

        public ImageProxyHandler(AppFunc next, IJabbrConfiguration configuration)
        {
            _next = next;
            _configuration = configuration;
        }

        public async Task Invoke(IDictionary<string, object> env)
        {
            var httpRequest = new OwinRequest(env);
            var httpResponse = new OwinResponse(env);

            string[] url;
            Uri uri;
            if (!httpRequest.GetQuery().TryGetValue("url", out url) ||
                url.Length == 0 ||
                String.IsNullOrEmpty(url[0]) ||
                !Uri.TryCreate(url[0], UriKind.Absolute, out uri) ||
                !ImageContentProvider.IsValidImagePath(uri) ||
                !IsAuthenticated(env))
            {
                httpResponse.StatusCode = 404;
                return;
            }

            try
            {
                var request = (HttpWebRequest)WebRequest.Create(uri);
                request.CachePolicy = new RequestCachePolicy(RequestCacheLevel.Default);
                var response = (HttpWebResponse)await request.GetResponseAsync();

                if (!ImageContentProvider.IsValidContentType(response.ContentType) &&
                    response.ContentLength > _configuration.ProxyImageMaxSizeBytes)
                {
                    httpResponse.StatusCode = 404;
                    return;
                }

                httpResponse.SetHeader("Content-Type", response.ContentType);
                httpResponse.StatusCode = (int)response.StatusCode;

                using (response)
                {
                    using (Stream stream = response.GetResponseStream())
                    {
                        await stream.CopyToAsync(httpResponse.Body);
                    }
                }
            }
            catch
            {
                httpResponse.StatusCode = 404;
            }
        }

        private static bool IsAuthenticated(IDictionary<string, object> env)
        {
            object principal;
            if (env.TryGetValue("server.User", out principal))
            {
                return ((IPrincipal)principal).IsAuthenticated();
            }
            return false;
        }
    }
}