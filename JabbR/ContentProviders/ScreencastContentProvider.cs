using System;
using System.IO;
using System.Threading.Tasks;
using HtmlAgilityPack;
using JabbR.ContentProviders.Core;
using JabbR.Infrastructure;
using Nancy.Helpers;

namespace JabbR.ContentProviders
{
    public class ScreencastContentProvider : CollapsibleContentProvider
    {
        private static readonly string ContentFormat = "<img src=\"{0}\" alt=\"{1}\" />";

        protected override Task<ContentProviderResult> GetCollapsibleContent(ContentProviderHttpRequest request)
        {
            return ExtractFromResponse(request).Then(pageInfo =>
            {
                return new ContentProviderResult
                {
                    Content = String.Format(ContentFormat,
                                            HttpUtility.HtmlAttributeEncode(pageInfo.ImageURL),
                                            HttpUtility.HtmlAttributeEncode(pageInfo.Title)),
                    Title = pageInfo.Title
                };
            });
        }

        public override bool IsValidContent(Uri uri)
        {
            return uri.Host.IndexOf("screencast.com", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private Task<PageInfo> ExtractFromResponse(ContentProviderHttpRequest request)
        {
            //Force https for the url
            var builder = new UriBuilder(request.RequestUri) { Scheme = "https" };

            return Http.GetAsync(builder.Uri).Then(response =>
            {
                var pageInfo = new PageInfo();
                using (Stream responseStream = response.GetResponseStream())
                {
                    var htmlDocument = new HtmlDocument();
                    htmlDocument.Load(responseStream);

                    HtmlNode title = htmlDocument.DocumentNode.SelectSingleNode("//title");
                    HtmlNode imageURL = htmlDocument.DocumentNode.SelectSingleNode("//img[@class='embeddedObject']");
                    pageInfo.Title = title != null ? title.InnerText : String.Empty;
                    pageInfo.ImageURL = imageURL != null ? imageURL.Attributes["src"].Value : String.Empty;
                }

                return pageInfo;
            });
        }

        private class PageInfo
        {
            public string Title { get; set; }
            public string ImageURL { get; set; }
        }
    }
}