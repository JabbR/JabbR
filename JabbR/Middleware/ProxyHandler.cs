using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Cache;
using System.Threading.Tasks;
using JabbR.ContentProviders;
using JabbR.Infrastructure;
using Owin.Types;

namespace JabbR
{
    using AppFunc = Func<IDictionary<string, object>, Task>;

    /// <summary>
    /// Proxies images through the jabbr server to avoid mixed mode https.
    /// </summary>
    public class ProxyHandler
    {
        private readonly AppFunc _next;
        private readonly string _path;

        public ProxyHandler(AppFunc next, string path)
        {
            _next = next;
            _path = path;
        }

        public Task Invoke(IDictionary<string, object> env)
        {
            var httpRequest = new OwinRequest(env);

            if (!httpRequest.Path.StartsWith(EnsureTrailingSlash(_path)))
            {
                return _next(env);
            }

            var httpResponse = new OwinResponse(env);

            var qs = new QueryStringCollection(httpRequest.QueryString);

            string url = qs["url"];

            Uri uri;
            if (String.IsNullOrEmpty(url) ||
                !ImageContentProvider.IsValidImagePath(url) ||
                !Uri.TryCreate(url, UriKind.Absolute, out uri))
            {
                httpResponse.StatusCode = 404;
                return TaskAsyncHelper.Empty;
            }

            var request = (HttpWebRequest)WebRequest.Create(url);
            request.CachePolicy = new RequestCachePolicy(RequestCacheLevel.Default);
            var requestTask = Task.Factory.FromAsync((cb, state) => request.BeginGetResponse(cb, state),
                                                                 ar => (HttpWebResponse)request.EndGetResponse(ar), null);

            return requestTask.Then(response =>
            {
                httpResponse.SetHeader("ContentType", response.ContentType);
                httpResponse.StatusCode = (int)response.StatusCode;

                using (response)
                {
                    using (Stream stream = response.GetResponseStream())
                    {
                        // TODO: Make this async
                        return stream.CopyToAsync(httpResponse.Body);
                    }
                }
            });
        }

        private static string EnsureTrailingSlash(string path)
        {
            return path.TrimEnd('/') + "/";
        }
    }
}