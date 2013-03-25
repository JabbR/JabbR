using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Cache;
using System.Security.Principal;
using System.Threading.Tasks;
using JabbR.ContentProviders;
using JabbR.Services;
using Owin.Types;

namespace JabbR.Middleware
{
    using AppFunc = Func<IDictionary<string, object>, Task>;

    /// <summary>
    /// Proxies images through the jabbr server to avoid mixed mode https.
    /// </summary>
    public class ImageProxyHandler
    {
        private readonly AppFunc _next;
        private readonly IApplicationSettings _settings;

        public ImageProxyHandler(AppFunc next, IApplicationSettings settings)
        {
            _next = next;
            _settings = settings;
        }

        public async Task Invoke(IDictionary<string, object> env)
        {
            var httpRequest = new Gate.Request(env);
            var httpResponse = new OwinResponse(env);

            string url;
            Uri uri;
            if (!httpRequest.Query.TryGetValue("url", out url) ||
                String.IsNullOrEmpty(url) ||
                !Uri.TryCreate(url, UriKind.Absolute, out uri) ||
                !ImageContentProvider.IsValidImagePath(uri) ||
                !IsAuthenticated(env))
            {
                httpResponse.StatusCode = 404;
                return;
            }

            try
            {
                var request = (HttpWebRequest)WebRequest.Create(url);
                request.CachePolicy = new RequestCachePolicy(RequestCacheLevel.Default);
                var response = (HttpWebResponse)await request.GetResponseAsync();

                if (!ImageContentProvider.IsValidContentType(response.ContentType) &&
                    response.ContentLength > _settings.ProxyImageMaxSizeBytes)
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
                return ((IPrincipal)principal).Identity.IsAuthenticated;
            }
            return false;
        }
    }
}