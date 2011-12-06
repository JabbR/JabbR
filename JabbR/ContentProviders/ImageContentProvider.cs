using System;
using System.Collections.Generic;
using System.Net;
using JabbR.ContentProviders.Core;

namespace JabbR.ContentProviders
{
    public class ImageContentProvider : CollapsibleContentProvider
    {
        private static readonly HashSet<string> _imageMimeTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase) {
            "image/png",
            "image/jpg",
            "image/jpeg",
            "image/bmp",
            "image/gif",
        };

        protected override ContentProviderResultModel GetCollapsibleContent(HttpWebResponse response)
        {
            return new ContentProviderResultModel()
             {
                 Content = String.Format(@"<img src=""{0}"" />", response.ResponseUri),
                 Title = response.ResponseUri.AbsoluteUri.ToString()
             };
        }

        protected override bool IsValidContent(HttpWebResponse response)
        {
            return !String.IsNullOrEmpty(response.ContentType) &&
                    _imageMimeTypes.Contains(response.ContentType);

        }
    }
}