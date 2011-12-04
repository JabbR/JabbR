using System;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;

namespace JabbR.ContentProviders
{
    public class JoinMeContentProvider : CollapsibleContentProvider
    {
        private static readonly Regex _joinMeIdRegex = new Regex(@"(\d+)");
        // 0 Lat, 1 Long, 2 Address, 3 Content Format for Info       
        private static readonly string _iframedMeetingFormat = "<iframe src=\"{0}\" width=\"700\" height=\"400\"></iframe>";
        protected override ContentProviderResultModel GetCollapsibleContent(HttpWebResponse response)
        {
            return new ContentProviderResultModel()
            {
                Content = String.Format(_iframedMeetingFormat, response.ResponseUri.AbsoluteUri),
                Title = "Join Me Meeting: " + response.ResponseUri.AbsoluteUri.ToString()
            };
        }

        protected string ExtractParameter(Uri responseUri)
        {
            return _joinMeIdRegex.Match(responseUri.AbsoluteUri)
                                .Groups
                                .Cast<Group>()
                                .Skip(1)
                                .Select(g => g.Value)
                                .Where(v => !String.IsNullOrEmpty(v))
                                .FirstOrDefault();
        }

        protected override bool IsValidContent(HttpWebResponse response)
        {
            return response.ResponseUri.AbsoluteUri.StartsWith("https://join.me/", StringComparison.OrdinalIgnoreCase);
        }
    }
}