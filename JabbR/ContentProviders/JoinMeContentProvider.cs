using System;
using System.Net;
using JabbR.ContentProviders.Core;

namespace JabbR.ContentProviders
{
    public class JoinMeContentProvider : CollapsibleContentProvider
    {
        private static readonly string _iframedMeetingFormat = "<iframe src=\"{0}\" width=\"700\" height=\"400\"></iframe>";

        protected override ContentProviderResultModel GetCollapsibleContent(HttpWebResponse response)
        {
            return new ContentProviderResultModel()
            {
                Content = String.Format(_iframedMeetingFormat, response.ResponseUri.AbsoluteUri),
                Title = "Join Me Meeting: " + response.ResponseUri.AbsoluteUri.ToString()
            };
        }

        protected override bool IsValidContent(HttpWebResponse response)
        {
            return response.ResponseUri.AbsoluteUri.StartsWith("https://join.me/", StringComparison.OrdinalIgnoreCase);
        }
    }
}