using System;
using System.Linq;
using System.Net;
using JabbR.ContentProviders.Core;

namespace JabbR.ContentProviders
{
    public class ImgurContentProvider : CollapsibleContentProvider
    {        
        protected override ContentProviderResultModel GetCollapsibleContent(Uri uri)
        {
            string id = uri.AbsoluteUri.Split('/').Last();

            return new ContentProviderResultModel()
            {
                Content = string.Format(@"<img src=""http://i.imgur.com/{0}.jpg"" />", id),
                Title = uri.AbsoluteUri
            };
        }

        protected override bool IsValidContent(Uri uri)
        {
            return uri.AbsoluteUri.StartsWith("http://imgur.com/", StringComparison.OrdinalIgnoreCase);
        }
    }
}