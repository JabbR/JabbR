using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Cache;
using System.Threading.Tasks;
using JabbR.ContentProviders;
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

        public ImageProxyHandler(AppFunc next)
        {
            _next = next;
        }

        public async Task Invoke(IDictionary<string, object> env)
        {
            var httpRequest = new Gate.Request(env);
            var httpResponse = new OwinResponse(env);

            string url;
            Uri uri;
            if (!httpRequest.Query.TryGetValue("url", out url) ||
                String.IsNullOrEmpty(url) ||
                !ImageContentProvider.IsValidImagePath(url) ||
                !Uri.TryCreate(url, UriKind.Absolute, out uri))
            {
                httpResponse.StatusCode = 404;
                return;
            }

            var request = (HttpWebRequest)WebRequest.Create(url);
            request.CachePolicy = new RequestCachePolicy(RequestCacheLevel.Default);
            var response = (HttpWebResponse)await request.GetResponseAsync();

            httpResponse.SetHeader("ContentType", response.ContentType);
            httpResponse.StatusCode = (int)response.StatusCode;

            using (response)
            {
                using (Stream stream = response.GetResponseStream())
                {
                    await stream.CopyToAsync(httpResponse.Body);
                }
            }
        }
    }
}