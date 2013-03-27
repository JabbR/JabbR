using System;
using System.ComponentModel.Composition;
using System.Threading.Tasks;
using JabbR.ContentProviders.Core;
using JabbR.Services;
using Microsoft.Security.Application;

namespace JabbR.ContentProviders
{
    public class ImageContentProvider : CollapsibleContentProvider
    {
        private readonly IApplicationSettings _settings;

        [ImportingConstructor]
        public ImageContentProvider(IApplicationSettings settings)
        {
            _settings = settings;
        }

        protected override Task<ContentProviderResult> GetCollapsibleContent(ContentProviderHttpRequest request)
        {
            string format = @"<a rel=""nofollow external"" target=""_blank"" href=""{0}""><img src=""{0}"" /></a>";
            if (_settings.ProxyImages)
            {
                // If we're proxying images, only proxy what we need to (non https images)
                if (_settings.RequireHttps &&
                    !request.RequestUri.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase))
                {
                    format = @"<a rel=""nofollow external"" target=""_blank"" href=""{0}""><img src=""proxy?url={0}"" /></a>";
                }
            }

            string url = request.RequestUri.ToString();
            return TaskAsyncHelper.FromResult(new ContentProviderResult()
            {
                Content = String.Format(format, Encoder.HtmlAttributeEncode(url)),
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
