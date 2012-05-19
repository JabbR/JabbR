using System;
using System.Threading.Tasks;
using JabbR.ContentProviders.Core;
using JabbR.Infrastructure;

namespace JabbR.ContentProviders
{
    public class UserVoiceContentProvider : CollapsibleContentProvider
    {
        private static readonly string _uservoiceAPIURL = "https://{0}/api/v1/oembed.json?url={1}";

        protected override Task<ContentProviderResult> GetCollapsibleContent(ContentProviderHttpRequest request)
        {
            return FetchArticle(request.RequestUri).Then(article =>
            {
                return new ContentProviderResult()
                {
                    Title = article.title,
                    Content = article.html
                };
            });
        }

        private static Task<dynamic> FetchArticle(Uri url)
        {
            return Http.GetJsonAsync(String.Format(_uservoiceAPIURL, url.Host, url.AbsoluteUri));
        }

        public override bool IsValidContent(Uri uri)
        {
            return uri.Host.IndexOf("uservoice.com", StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}