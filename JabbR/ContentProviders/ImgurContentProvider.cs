using System;
using System.Linq;
using System.Net;
using JabbR.ContentProviders.Core;

namespace JabbR.ContentProviders
{
    public class ImgurContentProvider : CollapsibleContentProvider
    {        
        protected override ContentProviderResultModel GetCollapsibleContent(HttpWebResponse response)
        {
            string id = response.ResponseUri.AbsoluteUri.Split('/').Last();

            return new ContentProviderResultModel()
            {
                Content = string.Format(@"<img src=""http://i.imgur.com/{0}.jpg"" />", id),
                Title = response.ResponseUri.AbsoluteUri.ToString()
            };
        }

        protected override bool IsValidContent(HttpWebResponse response)
        {
            return response.ResponseUri.AbsoluteUri.StartsWith("http://imgur.com/", StringComparison.OrdinalIgnoreCase);
        }
    }
}