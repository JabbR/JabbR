using System;
using System.Threading.Tasks;
using HtmlAgilityPack;
using JabbR.ContentProviders.Core;
using JabbR.Infrastructure;
using RestSharp.Extensions;

namespace JabbR.ContentProviders
{
    /// <summary>
    /// Content provider that will embed the XKCD comic linked
    /// </summary>
    public class XkcdContentProvider : CollapsibleContentProvider
    {
        private static readonly string ContentFormat = @"
<div class=""xkcd_wrapper"">
    <div class=""xkcd_header"">
        <a href=""{0}"" target=""_blank""><img src=""{1}"" title=""{2}"" /></a>
    </div>
</div>";

        protected override System.Threading.Tasks.Task<ContentProviderResult> GetCollapsibleContent(ContentProviderHttpRequest request)
        {
            return ExtractFromResponse(request).Then(pageInfo =>
            {
                if (pageInfo == null)
                {
                    return null;
                }

                return new ContentProviderResult
                {
                    Content = String.Format(ContentFormat, request.RequestUri.ToString(),  pageInfo.ImageUrl, pageInfo.Description),
                    Title = pageInfo.Title
                };
            });
        }

        private Task<XkcdContentProvider.XkcdComicInfo> ExtractFromResponse(ContentProviderHttpRequest request)
        {
            return Http.GetAsync(request.RequestUri).Then(response =>
            {
                var comicInfo = new XkcdComicInfo();

                using (var responseStream = response.GetResponseStream())
                {
                    var htmlDocument = new HtmlDocument();
                    htmlDocument.Load(responseStream);
                    htmlDocument.OptionFixNestedTags = true;

                    var comic = htmlDocument.DocumentNode.SelectSingleNode("//div[@id='comic']/img");
                                            
                    if (comic == null)
                    {
                        return null;
                    }

                    comicInfo.Title = comic.Attributes["alt"].Value;
                    comicInfo.ImageUrl = comic.Attributes["src"].Value;
                    comicInfo.Description = comic.Attributes["title"].Value;

                }

                return comicInfo;
            });
        }

        public override bool IsValidContent(Uri uri)
        {
            return uri.AbsoluteUri.Matches(@"https?:\/\/xkcd.com\/\d+\/?");
        }


        private class XkcdComicInfo
        {
            public string Title { get; set; }
            public string ImageUrl { get; set; }
            public string Description { get; set; }

        }
    }
}
