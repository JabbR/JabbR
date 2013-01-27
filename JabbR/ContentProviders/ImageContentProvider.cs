using System;
using System.Threading.Tasks;
using JabbR.ContentProviders.Core;
using Microsoft.Security.Application;

namespace JabbR.ContentProviders
{
    public class ImageContentProvider : CollapsibleContentProvider
    {
        protected override Task<ContentProviderResult> GetCollapsibleContent(ContentProviderHttpRequest request)
        {
            string url = request.RequestUri.ToString();
            return TaskAsyncHelper.FromResult(new ContentProviderResult()
             {
                 Content = String.Format(@"<img src=""proxy?url={0}"" />", Encoder.HtmlAttributeEncode(url)),
                 Title = url
             });
        }

        public override bool IsValidContent(Uri uri)
        {
            return IsValidImagePath(uri);
        }

        public static bool IsValidImagePath(Uri uri)
        {
            string path = uri.LocalPath.ToLowerInvariant();

            return path.EndsWith(".png") ||
                   path.EndsWith(".bmp") ||
                   path.EndsWith(".jpg") ||
                   path.EndsWith(".jpeg") ||
                   path.EndsWith(".gif");
        }

        public static bool IsValidContentType(string contentType)
        {
            return contentType.Equals("image/bmp", StringComparison.OrdinalIgnoreCase) ||
                   contentType.Equals("image/gif", StringComparison.OrdinalIgnoreCase) ||
                   contentType.Equals("image/jpeg", StringComparison.OrdinalIgnoreCase) ||
                   contentType.Equals("image/tiff", StringComparison.OrdinalIgnoreCase) ||
                   contentType.Equals("image/x-tiff", StringComparison.OrdinalIgnoreCase) ||
                   contentType.Equals("image/png", StringComparison.OrdinalIgnoreCase) ||
                   contentType.Equals("image/svg+xml", StringComparison.OrdinalIgnoreCase);                   
        }
    }
}