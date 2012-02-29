using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Web;
using JabbR.ContentProviders.Core;
using System.Threading.Tasks;
using JabbR.Infrastructure;

namespace JabbR.ContentProviders
{
    public class BBCContentProvider : CollapsibleContentProvider
    {
        private static readonly string ContentFormat = "<div class='bbc_wrapper'><div class=\"bbc_header\"><img src=\"/Content/images/contentproviders/bbcnews-masthead.png\" alt=\"\" width=\"84\" height=\"24\"></div><img src=\"{1}\" title=\"{2}\" alt=\"{3}\" class=\"bbc_newsimage\" /><h2>{0}</h2><div>{4}</div><div><a href=\"{5}\" target=\"_blank\">View article</a></div></div>";

        protected override Task<ContentProviderResult> GetCollapsibleContent(ContentProviderHttpRequest request)
        {
            return ExtractFromResponse(request).Then(pageInfo =>
            {
                return new ContentProviderResult()
                {
                    Content = String.Format(ContentFormat, pageInfo.Title, pageInfo.ImageURL, pageInfo.Title, pageInfo.Title, pageInfo.Description, pageInfo.PageURL),
                    Title = pageInfo.Title
                };
            });
        }

        private Task<PageInfo> ExtractFromResponse(ContentProviderHttpRequest request)
        {
            return Http.GetAsync(request.RequestUri).Then(response =>
            {
                var info = new PageInfo();
                using (var responseStream = response.GetResponseStream())
                {
                    using (var sr = new StreamReader(responseStream))
                    {
                        var pageContext = HttpUtility.HtmlDecode(sr.ReadToEnd());
                        info.Title = ExtractUsingRegex(new Regex(@"<meta\s.*property=""og:title"".*content=""(.*)"".*/>"), pageContext);
                        info.Description = ExtractUsingRegex(new Regex(@"<meta\s.*name=""Description"".*content=""(.*)"".*/>"), pageContext);
                        info.ImageURL = ExtractUsingRegex(new Regex(@"<meta.*property=""og:image"".*content=""(.*)"".*/>"), pageContext);
                        info.PageURL = request.RequestUri.AbsoluteUri;
                    }
                }

                return info;
            });
        }

        private string ExtractUsingRegex(Regex regularExpression, string content)
        {
            var matches = regularExpression.Match(content)
                .Groups
                .Cast<Group>()
                .Skip(1)
                .Select(g => g.Value)
                .Where(v => !String.IsNullOrEmpty(v));

            return matches.FirstOrDefault() ?? String.Empty;
        }

        private class PageInfo
        {
            public string Title { get; set; }
            public string Description { get; set; }
            public string ImageURL { get; set; }
            public string PageURL { get; set; }
        }

        public override bool IsValidContent(Uri uri)
        {
            return uri.AbsoluteUri.StartsWith("http://www.bbc.co.uk/news", StringComparison.OrdinalIgnoreCase);
        }
    }
}