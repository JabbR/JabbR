using System;
using System.IO;
using System.Net;
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

            // TODO: Add caching
            var request = (HttpWebRequest)WebRequest.Create(url);
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