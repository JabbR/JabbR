using System;
using System.Threading.Tasks;
using JabbR.ContentProviders.Core;

namespace JabbR.ContentProviders
{
    public class JoinMeContentProvider : CollapsibleContentProvider
    {
        private static readonly string _iframedMeetingFormat = "<iframe src=\"{0}\" width=\"700\" height=\"400\"></iframe>";

        protected override Task<ContentProviderResult> GetCollapsibleContent(ContentProviderHttpRequest request)
        {
            return TaskAsyncHelper.FromResult(new ContentProviderResult()
            {
                Content = String.Format(_iframedMeetingFormat, request.RequestUri.AbsoluteUri),
                Title = String.Format(LanguageResources.JoinMeContent_DefaultTitle, request.RequestUri.AbsoluteUri)
            });
        }

        public override bool IsValidContent(Uri uri)
        {
            return uri.AbsoluteUri.StartsWith("https://join.me/", StringComparison.OrdinalIgnoreCase);
        }
    }
}