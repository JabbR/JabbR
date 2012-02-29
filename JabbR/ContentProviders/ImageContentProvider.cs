using System;
using System.Collections.Generic;
using System.Net;
using JabbR.ContentProviders.Core;

namespace JabbR.ContentProviders
{
    public class ImageContentProvider : CollapsibleContentProvider
    {
        protected override ContentProviderResultModel GetCollapsibleContent(Uri uri)
        {
            return new ContentProviderResultModel()
             {
                 Content = String.Format(@"<img src=""{0}"" />", uri),
                 Title = uri.AbsoluteUri
             };
        }

        protected override bool IsValidContent(Uri uri)
        {
            string path = uri.AbsolutePath.ToLower();
            return path.EndsWith(".png") ||
                   path.EndsWith(".bmp") ||
                   path.EndsWith(".jpg") ||
                   path.EndsWith(".jpeg") ||
                   path.EndsWith(".gif");
        }
    }
}