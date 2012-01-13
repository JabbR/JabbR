using System;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using JabbR.ContentProviders.Core;

namespace JabbR.ContentProviders
{
    public class BashQDBContentProvider : CollapsibleContentProvider
    {
        private static readonly Regex QueryRegex = new Regex(@"\d+");
        private static readonly string ContentFormat = @"
<div class=""bashqdb_wrapper"">
    <div class=""bashqdb_header"">
        <a href=""{0}"" target=""_blank"">#{1}</a>
    </div>
    <div class=""bashqdb_content"">{2}</div>
</div>";

        protected override ContentProviderResultModel GetCollapsibleContent(HttpWebResponse response)
        {
            var pageInfo = ExtractFromResponse(response);

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
                                        .Single().InnerHtml;

                var title = htmlDocument.DocumentNode
                                        .SelectSingleNode("//title")
                                        .InnerText;

                info.Quote = quote;
                info.PageURL = response.ResponseUri.AbsoluteUri;
                info.QuoteNumber = title;
            }

            return info;
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