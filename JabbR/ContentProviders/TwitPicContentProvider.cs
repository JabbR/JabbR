using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using JabbR.ContentProviders.Core;

namespace JabbR.ContentProviders
{
    public class TwitPicContentProvider : CollapsibleContentProvider
    {
        static Regex TwitPicUrlRegex = new Regex(@"^http://(www\.)?twitpic\.com/(?<Id>\w+)", RegexOptions.IgnoreCase);

        private readonly string _twitPicFormatString = @"<a href=""http://twitpic.com/{0}""> <img src=""http://twitpic.com/show/large/{0}""></a>";

        protected override Task<ContentProviderResult> GetCollapsibleContent(ContentProviderHttpRequest request)
        {
            var match = TwitPicUrlRegex.Match(request.RequestUri.AbsoluteUri);

            if (match.Success)
            {
                var id = match.Groups["Id"].Value;
                return TaskAsyncHelper.FromResult(new ContentProviderResult()
                {
                    Content = String.Format(_twitPicFormatString, id),
                    Title = request.RequestUri.AbsoluteUri
                });
            }

            return TaskAsyncHelper.FromResult<ContentProviderResult>(null);
        }

        public override bool IsValidContent(Uri uri)
        {
            return TwitPicUrlRegex.IsMatch(uri.AbsoluteUri);
        }
    }
}