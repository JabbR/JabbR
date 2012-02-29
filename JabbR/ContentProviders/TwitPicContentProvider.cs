using System;
using System.Net;
using System.Text.RegularExpressions;
using JabbR.ContentProviders.Core;

namespace JabbR.ContentProviders
{
    public class TwitPicContentProvider:CollapsibleContentProvider
    {
        static Regex TwitPicUrlRegex = new Regex(@"^http://(www\.)?twitpic\.com/(?<Id>\w+)", RegexOptions.IgnoreCase);

        private readonly string _twitPicFormatString = @"<a href=""http://twitpic.com/{0}""> <img src=""http://twitpic.com/show/large/{0}""></a>";

        protected override ContentProviderResultModel GetCollapsibleContent(HttpWebResponse response)
        {
            var match = TwitPicUrlRegex.Match(response.ResponseUri.AbsoluteUri);

            if (match.Success)
            {
                var id = match.Groups["Id"].Value;
                return new ContentProviderResultModel()
                {
                    Content = String.Format(_twitPicFormatString, id),
                    Title = response.ResponseUri.AbsoluteUri
                };
            }
            return null;
        }

        protected override bool IsValidContent(HttpWebResponse response)
        {
            return TwitPicUrlRegex.IsMatch(response.ResponseUri.AbsoluteUri);
        }
    }
}