using System;
using System.IO;
using System.Threading.Tasks;
using JabbR.ContentProviders.Core;
using Newtonsoft.Json;
using JabbR.Infrastructure;

namespace JabbR.ContentProviders
{
    public class SoundCloudContentProvider : CollapsibleContentProvider
    {
        protected override Task<ContentProviderResult> GetCollapsibleContent(ContentProviderHttpRequest request)
        {
            var url = String.Format(@"https://soundcloud.com/oembed?format=json&iframe=true&show_comments=false&url={0}", request.RequestUri.AbsoluteUri);

            return Http.GetJsonAsync<SoundCloudResponse>(url).Then(widgetInfo =>
            {
                string content = widgetInfo.FrameMarkup.Replace("src=\"http://", "src=\"//");
                return new ContentProviderResult
                {
                    Title = widgetInfo.Title,
                    Content = content
                };
            });
        }

        public override bool IsValidContent(Uri uri)
        {
            return uri.Host.IndexOf("soundcloud.com", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private sealed class SoundCloudResponse
        {
            [JsonProperty(PropertyName = "title")]
            public string Title { get; set; }
            [JsonProperty(PropertyName = "html")]
            public string FrameMarkup { get; set; }
        }
    }
}