using System;
using System.IO;
using System.Net;
using System.Net.Cache;
using System.Threading.Tasks;
using System.Web;
using JabbR.ContentProviders;
using SignalR.Hosting.AspNet;

namespace JabbR.Auth
{
    /// <summary>
    /// Proxies images through the jabbr server to avoid mixed mode https.
    /// </summary>
    public class ProxyHandler : HttpTaskAsyncHandler
    {
        public override Task ProcessRequestAsync(HttpContextBase context)
        {
            string url = context.Request.QueryString["url"];

            Uri uri;
            if (String.IsNullOrEmpty(url) ||
                !ImageContentProvider.IsValidImagePath(url) ||
                !Uri.TryCreate(url, UriKind.Absolute, out uri))
            {
                context.Response.StatusCode = 404;
                return TaskAsyncHelper.Empty;
            }

            // Since we only handle requests for imgur and other random images, just cached based on the url
            // context.Response.Cache.SetCacheability(HttpCacheability.Public);
            // context.Response.Cache.SetMaxAge(TimeSpan.MaxValue);
            // context.Response.Cache.SetLastModified(DateTime.Now);

            var request = (HttpWebRequest)WebRequest.Create(url);
            request.CachePolicy = new RequestCachePolicy(RequestCacheLevel.Default);
            var requestTask = Task.Factory.FromAsync((cb, state) => request.BeginGetResponse(cb, state),
                                                                 ar => (HttpWebResponse)request.EndGetResponse(ar), null);

            return requestTask.Then(response =>
            {
                context.Response.ContentType = response.ContentType;
                context.Response.StatusCode = (int)response.StatusCode;
                context.Response.StatusDescription = response.StatusDescription;

                using (response)
                {
                    using (Stream stream = response.GetResponseStream())
                    {
                        // TODO: Make this async
                        stream.CopyTo(context.Response.OutputStream);
                    }
                }
            });
        }
    }
}