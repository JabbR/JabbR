using System;
using System.Linq;
using System.Net;

namespace SignalR.Samples.Hubs.Chat.ContentProviders
{
    public class ImgurContentProvider : CollapsibleContentProvider
    {
        protected override string GetTitle(HttpWebResponse response)
        {
            return response.ResponseUri.ToString();
        }

        protected override string GetCollapsibleContent(HttpWebResponse response)
        {
            string id = response.ResponseUri.AbsoluteUri.Split('/').Last();

            return string.Format(@"<img src=""http://i.imgur.com/{0}.jpg"" />", id);
        }

        protected override bool IsValidContent(HttpWebResponse response)
        {
            return response.ResponseUri.AbsoluteUri.StartsWith("http://imgur.com/", StringComparison.OrdinalIgnoreCase);
        }
    }
}