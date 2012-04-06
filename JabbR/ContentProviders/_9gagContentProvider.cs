using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using JabbR.ContentProviders.Core;

namespace JabbR.ContentProviders
{
    public class _9gagContentProvider : CollapsibleContentProvider
    {
        static Regex _9gagUrlRegex = new Regex(@"^http://(www\.)?9gag\.com/gag/(?<Id>\w+)", RegexOptions.IgnoreCase);

        private readonly string _9gagFormatString = @"<a href=""http://9gag.com/gag/{0}""> <img src=""http://d24w6bsrhbeh9d.cloudfront.net/photo/{0}_700b.jpg""></a>";

        protected override Task<ContentProviderResult> GetCollapsibleContent(ContentProviderHttpRequest request)
        {
            var match = _9gagUrlRegex.Match(request.RequestUri.AbsoluteUri);

            if (match.Success)
            {
                var id = match.Groups["Id"].Value;
                return TaskAsyncHelper.FromResult(new ContentProviderResult()
                {
                    Content = String.Format(_9gagFormatString, id),
                    Title = request.RequestUri.AbsoluteUri
                });
            }

            return TaskAsyncHelper.FromResult<ContentProviderResult>(null);
        }

        public override bool IsValidContent(Uri uri)
        {
            return _9gagUrlRegex.IsMatch(uri.AbsoluteUri);
        }
    }
}