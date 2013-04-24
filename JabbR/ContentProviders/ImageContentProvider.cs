using System;
using System.ComponentModel.Composition;
using System.IO;
using System.Threading.Tasks;
using JabbR.ContentProviders.Core;
using JabbR.Infrastructure;
using JabbR.Services;
using JabbR.UploadHandlers;
using Microsoft.Security.Application;
using Ninject;

namespace JabbR.ContentProviders
{
    public class ImageContentProvider : CollapsibleContentProvider
    {
        private readonly IKernel _kernel;
        private readonly IJabbrConfiguration _configuration;

        [ImportingConstructor]
        public ImageContentProvider(IKernel kernel)
        {
            _kernel = kernel;
            _configuration = kernel.Get<IJabbrConfiguration>();
        }

        protected override async Task<ContentProviderResult> GetCollapsibleContent(ContentProviderHttpRequest request)
        {
            string format = @"<a rel=""nofollow external"" target=""_blank"" href=""{0}""><img src=""{0}"" /></a>";
            string url = request.RequestUri.ToString();

            // Only proxy what we need to (non https images)
            if (_configuration.RequireHttps &&
                !request.RequestUri.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase))
            {
                var uploadProcessor = _kernel.Get<UploadProcessor>();
                var response = await Http.GetAsync(request.RequestUri);
                string fileName = Path.GetFileName(request.RequestUri.LocalPath);
                string contentType = GetContentType(request.RequestUri);
                long contentLength = response.ContentLength;

                using (Stream stream = response.GetResponseStream())
                {
                    UploadResult result = await uploadProcessor.HandleUpload(fileName, contentType, stream, contentLength);

                    if (result != null)
                    {
                        url = result.Url;
                    }
                }
            }

            return new ContentProviderResult()
            {
                Content = String.Format(format, Encoder.HtmlAttributeEncode(url)),
                Title = url
            };
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

        public static string GetContentType(Uri uri)
        {
            string extension = Path.GetExtension(uri.LocalPath).ToLowerInvariant();

            switch (extension)
            {
                case ".png":
                    return "image/png";
                case ".bmp":
                    return "image/bmp";
                case ".gif":
                    return "image/gif";
                case ".jpg":
                case ".jpeg":
                    return "image/jpeg";
            }

            return null;
        }
    }
}
