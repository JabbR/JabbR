using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Web;
using JabbR.ContentProviders.Core;

namespace JabbR.ContentProviders {
    public class BBCContentProvider : CollapsibleContentProvider {
        private const string ContentFormat =
            "<div class='bbc_wrapper'><div style='background-color:#990000;background-position: top center; background-image: url(\"http://news.bbcimg.co.uk/view/1_4_29/cream/hi/news/img/red-masthead.png\"); background-repeat:no-repeat; color:white;font-family:\"Arial Black\";letter-spacing:-0.1em; font-weight:bold; padding-left:3px; padding-top:3px;'><img src=\"http://static.bbc.co.uk/frameworks/barlesque/1.21.2/desktop/3/img/blocks/light.png\" alt=\"\" width=\"84\" height=\"24\"></div><img src=\"{1}\" title=\"{2}\" align=\"left\" alt=\"{3}\" style=\"margin-right:5px;margin-bottom:5px;margin-top:3px;\" /><h2 style=\"margin-top:3px\">{0}</h2><div>{4}</div><div><a href=\"{5}\">View article</a></div></div>";

        protected override ContentProviderResultModel GetCollapsibleContent(HttpWebResponse response) {
            var pageInfo = ExtractFromResponse(response);
            return new ContentProviderResultModel() {
                Content = string.Format(ContentFormat, pageInfo.Title, pageInfo.ImageUrl, pageInfo.Title, pageInfo.Title, pageInfo.Description, pageInfo.PageURL),
                Title = pageInfo.Title
            };
        }

        private PageInfo ExtractFromResponse(HttpWebResponse response) {
            var info = new PageInfo();
            using (var responseStream = response.GetResponseStream()) {
                using (var sr = new StreamReader(responseStream)) {
                    var pageContext = HttpUtility.HtmlDecode(sr.ReadToEnd());
                    info.Title = extractUsingRegEx(new Regex(@"<meta\s.*property=""og:title"".*content=""(.*)"".*/>"), pageContext);
                    info.Description = extractUsingRegEx(new Regex(@"<meta\s.*name=""Description"".*content=""(.*)"".*/>"), pageContext);
                    info.ImageUrl = extractUsingRegEx(new Regex(@"<meta.*property=""og:image"".*content=""(.*)"".*/>"), pageContext);
                    info.PageURL = response.ResponseUri.AbsoluteUri.ToString();
                }
            }
            return info;
        }

        string extractUsingRegEx(Regex regularExpression, string content) {
            var matches = regularExpression.Match(content)
                                        .Groups
                                        .Cast<Group>()
                                        .Skip(1)
                                        .Select(g => g.Value)
                                        .Where(v => !String.IsNullOrEmpty(v));

            return matches.FirstOrDefault() ?? String.Empty;
        }

        class PageInfo {
            public string Title { get; set; }
            public string Description { get; set; }
            public string ImageUrl { get; set; }
            public string PageURL { get; set; }
        }

        protected override bool IsValidContent(HttpWebResponse response) {
            return response.ResponseUri.AbsoluteUri.StartsWith("http://www.bbc.co.uk/news", StringComparison.OrdinalIgnoreCase);
        }
    }
}