using System;
using System.Net;
using JabbR.ContentProviders.Core;

namespace JabbR.ContentProviders
{
    public class JoinMeContentProvider : CollapsibleContentProvider
    {
        private static readonly string _iframedMeetingFormat = "<iframe src=\"{0}\" width=\"700\" height=\"400\"></iframe>";

        protected override ContentProviderResultModel GetCollapsibleContent(Uri uri)
        {
            return new ContentProviderResultModel()
            {
                Content = String.Format(_iframedMeetingFormat, uri.AbsoluteUri),
                Title = "Join Me Meeting: " + uri.AbsoluteUri
            };
        }

        protected override bool IsValidContent(Uri uri)
        {
            return uri.AbsoluteUri.StartsWith("https://join.me/", StringComparison.OrdinalIgnoreCase);
        }
    }
}