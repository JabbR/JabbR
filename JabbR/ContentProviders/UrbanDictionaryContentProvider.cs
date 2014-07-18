using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using JabbR.ContentProviders.Core;
using JabbR.Infrastructure;

namespace JabbR.ContentProviders
{
    public class UrbanDictionaryContentProvider : CollapsibleContentProvider
    {
        private static readonly string _contentFormat = @"
        <article class=""urban-dictionary"">
            <div class=""word"">
                <a href=""{0}"" target=""_blank"">{1}</a>
            </div>
            <div class=""meaning"">{2}</div>
            <div class=""example"">{3}</div>
        </div>";

        private static readonly string _urbanDictionaryAPIURL = "http://api.urbandictionary.com/v0/define?{0}{1}";

        protected override Task<ContentProviderResult> GetCollapsibleContent(ContentProviderHttpRequest request)
        {
            var parameters = new QueryStringCollection(request.RequestUri);

            // Extract either the term or defid field from request.RequestUri
            string defid = parameters["defid"];
            string term = parameters["term"];

            if (string.IsNullOrWhiteSpace(term) && string.IsNullOrWhiteSpace(defid))
            {
                return null;
            }

            var apiQuery = string.Format(_urbanDictionaryAPIURL,
                string.IsNullOrWhiteSpace(term) ? string.Empty : string.Format("term={0}", term),
                string.IsNullOrWhiteSpace(defid) ? string.Empty : string.Format("&defid={0}", defid));

            return FetchArticle(new Uri(apiQuery)).Then(result =>
            {
                var list = result.list;

                var count = list.Count;

                if (count > 0)
                {
                    string definition = list[0].definition;
                    string permalink = list[0].permalink;
                    string title = list[0].word;
                    string example = list[0].example;

                    return new ContentProviderResult
                    {
                        Title = title,
                        Content = string.Format(_contentFormat, permalink, title, definition.Replace("\n", "<br/>"), example.Replace("\n", "<br/>"))
                    };
                }

                return null;
            });
        }

        private static Task<dynamic> FetchArticle(Uri url)
        {
            return Http.GetJsonAsync(url.AbsoluteUri);
        }

        public override bool IsValidContent(Uri uri)
        {
            return uri.Host.IndexOf("urbandictionary.com", StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}