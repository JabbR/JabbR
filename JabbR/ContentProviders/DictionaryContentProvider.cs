using System;
using System.Threading.Tasks;
using HtmlAgilityPack;
using JabbR.ContentProviders.Core;
using JabbR.Infrastructure;

namespace JabbR.ContentProviders
{
    public class DictionaryContentProvider : CollapsibleContentProvider
    {
        private const string _domain = "http://dictionary.reference.com";
        private static readonly string ContentFormat = "<div class='dictionary_wrapper'>" +
                                                       "    <div class=\"dictionary_header\">" +
                                                       "        <img src=\"{2}\" alt=\"\" width=\"64\" height=\"64\">" +
                                                       "        <h2>{0}</h2>" +
                                                       "    </div>" +
                                                       "    <div>{1}</div>" +
                                                       "</div>";

        protected override Task<ContentProviderResult> GetCollapsibleContent(ContentProviderHttpRequest request)
        {
            return ExtractFromResponse(request).Then(pageInfo =>
            {
                return new ContentProviderResult
                {
                    Content = String.Format(ContentFormat, pageInfo.Title, pageInfo.WordDefinition, pageInfo.ImageURL),
                    Title = pageInfo.Title
                };
            });
        }

        public override bool IsValidContent(Uri uri)
        {
            return uri.AbsoluteUri.StartsWith("http://dictionary.reference.com", StringComparison.OrdinalIgnoreCase) ||
                   uri.AbsoluteUri.StartsWith("http://dictionary.com", StringComparison.OrdinalIgnoreCase);
        }

        private Task<PageInfo> ExtractFromResponse(ContentProviderHttpRequest request)
        {
            return Http.GetAsync(request.RequestUri).Then(response =>
            {
                var pageInfo = new PageInfo();
                using (var responseStream = response.GetResponseStream())
                {
                    var htmlDocument = new HtmlDocument();
                    htmlDocument.Load(responseStream);

                    var title = htmlDocument.DocumentNode.SelectSingleNode("//meta[@property='og:title']");
                    var imageURL = htmlDocument.DocumentNode.SelectSingleNode("//meta[@property='og:image']");
                    pageInfo.Title = title != null ? title.Attributes["content"].Value : String.Empty;
                    pageInfo.ImageURL = imageURL != null ? imageURL.Attributes["content"].Value : String.Empty;
                    pageInfo.WordDefinition = GetWordDefinition(htmlDocument);
                }

                return pageInfo;
            });
        }

        private string GetWordDefinition(HtmlDocument htmlDocument)
        {
            var wordDefinition = htmlDocument.DocumentNode.SelectSingleNode("//div[@class=\"body\"]");
            if (wordDefinition == null)
            {
                return String.Empty;
            }

            //remove stylesheet links
            var stylesheets = wordDefinition.SelectNodes("//link");
            foreach (var stylesheet in stylesheets)
            {
                stylesheet.Remove();
            }

            // fix relative url
            var links = wordDefinition.SelectNodes("//a");

            foreach (var link in links)
            {
                var href = link.Attributes["href"];
                if (href != null && href.Value.StartsWith("/"))
                {
                    href.Value = String.Format("{0}{1}", _domain, href.Value);

                    if (link.Attributes["style"] != null)
                    {
                        link.Attributes["style"].Value = String.Empty;
                    }

                    link.SetAttributeValue("target", "_blank");
                }
                else
                {
                    link.Remove();
                }
            }

            return wordDefinition.InnerHtml;
        }

        private class PageInfo
        {
            public string Title { get; set; }
            public string ImageURL { get; set; }
            public string WordDefinition { get; set; }
        }
    }
}