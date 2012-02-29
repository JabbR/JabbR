using System;
using System.Linq;
using System.Net;
using HtmlAgilityPack;
using JabbR.ContentProviders.Core;

namespace JabbR.ContentProviders
{
    public class BashQDBContentProvider : CollapsibleContentProvider
    {
        private static readonly string ContentFormat = @"
<div class=""bashqdb_wrapper"">
    <div class=""bashqdb_header"">
        <a href=""{0}"" target=""_blank"">{1}</a>
    </div>
    <div class=""bashqdb_content"">{2}</div>
</div>";

        private static readonly string[] WhiteListHtml = new[] {"br", "#text"};

        protected override ContentProviderResultModel GetCollapsibleContent(HttpWebResponse response)
        {
            var pageInfo = ExtractFromResponse(response);

            if (pageInfo == null)
            {
                return null;
            }

            return new ContentProviderResultModel
                       {
                           Content = String.Format(ContentFormat, pageInfo.PageURL, pageInfo.QuoteNumber, pageInfo.Quote),
                           Title = pageInfo.PageURL
                       };
        }

        private PageInfo ExtractFromResponse(HttpWebResponse response)
        {
            var info = new PageInfo();
            
            using (var responseStream = response.GetResponseStream())
            {
                var htmlDocument = new HtmlDocument();
                htmlDocument.Load(responseStream);
                htmlDocument.OptionFixNestedTags = true;

                var quote = htmlDocument.DocumentNode
                                        .SelectSingleNode("//body")
                                        .SelectNodes("//p").Where(a => a.Attributes.Any(x => x.Name == "class" && x.Value == "qt"))
                                        .SingleOrDefault();

                var title = htmlDocument.DocumentNode
                                        .SelectSingleNode("//title");

                //Quote might not be found, bash.org doesn't have a 404 page
                if (quote == null || title == null)
                {
                    return null;
                }

                //Strip out any HTML that isn't defined in the WhiteList
                SanitizeHtml(quote);

                info.Quote = quote.InnerHtml;
                info.PageURL = response.ResponseUri.AbsoluteUri;
                info.QuoteNumber = title.InnerHtml;
            }

            return info;
        }

        private void SanitizeHtml(HtmlNode quote)
        {
            for (int i = quote.ChildNodes.Count - 1; i >= 0; i--)
            {
                var nodeName = quote.ChildNodes[i].Name;
                if (!WhiteListHtml.Contains(nodeName))
                {
                    quote.ChildNodes[i].Remove();
                }
            }
        }

        private class PageInfo
        {
            public string PageURL { get; set; }
            public string Quote { get; set; }
            public string QuoteNumber { get; set; }
        }

        protected override bool IsValidContent(HttpWebResponse response)
        {
            var uri = response.ResponseUri.AbsoluteUri;

            return uri.StartsWith("http://www.bash.org/?", StringComparison.OrdinalIgnoreCase)
                   || uri.StartsWith("http://bash.org/?", StringComparison.OrdinalIgnoreCase);

        }
    }
}