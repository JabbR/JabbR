using System;
using System.Linq;
using System.Threading.Tasks;
using JabbR.ContentProviders.Core;

namespace JabbR.ContentProviders
{
    public class ImgurContentProvider : CollapsibleContentProvider
    {
        protected override Task<ContentProviderResult> GetCollapsibleContent(ContentProviderHttpRequest request)
        {
            string id = request.RequestUri.AbsoluteUri.Split('/').Last();

            return TaskAsyncHelper.FromResult(new ContentProviderResult()
            {
                Content = String.Format(@"<img src=""https://i.imgur.com/{0}.png"" />", id),
                Title = request.RequestUri.AbsoluteUri
            });
        }

        public override bool IsValidContent(Uri uri)
        {
            // not perfect, we have no way of differentiating eg http://imgur.com/random from a valid page
            // we should also look at rewriting non-ssl images from imgur to https://i.imgur.com rather than proxying at some point.
            bool isImgurDomain = uri.Host.Equals("imgur.com", StringComparison.OrdinalIgnoreCase) || 
                uri.Host.Equals("www.imgur.com", StringComparison.OrdinalIgnoreCase) ||
                uri.Host.Equals("i.imgur.com", StringComparison.OrdinalIgnoreCase);
            return isImgurDomain &&
                !uri.AbsolutePath.StartsWith("/a/", StringComparison.OrdinalIgnoreCase) &&
                !uri.AbsolutePath.Equals("/", StringComparison.OrdinalIgnoreCase) &&
                !uri.AbsolutePath.Contains(".");
        }
    }
}