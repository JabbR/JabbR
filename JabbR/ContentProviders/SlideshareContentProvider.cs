using System;
using System.Threading.Tasks;
using JabbR.ContentProviders.Core;
using JabbR.Infrastructure;

namespace JabbR.ContentProviders
{
    /// <summary>
    /// Content Provider that provides the necessary functionality to show SlideShare embed code in the chat when someone pastes a
    /// slideshare link (eg. http://www.slideshare.net/verticalmeasures/search-social-and-content-marketing-move-your-business-forward-accelerate). 
    /// Send feedback/issues to Seth Webster (@sethwebsterDanTup on Twitter).
    /// </summary>
    public class SlideShareContentProvider : CollapsibleContentProvider
    {
        private static readonly String _oEmbedUrl = "https://www.slideshare.net/api/oembed/2?url={0}&format=json";


        protected override Task<ContentProviderResult> GetCollapsibleContent(ContentProviderHttpRequest request)
        {
            // We have to make a call to the SlideShare api because
            // their embed code request the unique ID of the slide deck
            // where we will only have the url -- this call gets the json information
            // on the slide deck and that package happens to already contain the embed code (.html)
            var url = String.Format(_oEmbedUrl, request.RequestUri.AbsoluteUri);
            return Http.GetJsonAsync(url).Then(slideShareData =>
            {
                string html = slideShareData.html;
                return new ContentProviderResult()
                {
                    Content = html.Replace("http://", "https://"),
                    Title = slideShareData.title
                };
            });
        }

        public override bool IsValidContent(Uri uri)
        {
            return uri.AbsoluteUri.StartsWith("http://slideshare.net/", StringComparison.OrdinalIgnoreCase)
               || uri.AbsoluteUri.StartsWith("http://www.slideshare.net/", StringComparison.OrdinalIgnoreCase);
        }
    }
}