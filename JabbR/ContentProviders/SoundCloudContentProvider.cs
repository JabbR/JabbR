using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using JabbR.ContentProviders.Core;
using Newtonsoft.Json;

namespace JabbR.ContentProviders
{
    public class SoundCloudContentProvider : CollapsibleContentProvider
    {
        protected override ContentProviderResultModel GetCollapsibleContent(HttpWebResponse response)
        {
            SoundCloudResponse widgetInfo = null;

            var parentTask = new Task(
                () =>
                    {
                        var webRequest =
                            WebRequest.Create(
                                string.Format(
                                    @"http://soundcloud.com/oembed?format=json&iframe=true&show_comments=false&url={0}",
                                    response.ResponseUri.AbsoluteUri));

                        var task = Task<WebResponse>.Factory.FromAsync(
                            webRequest.BeginGetResponse, webRequest.EndGetResponse,
                            TaskCreationOptions.AttachedToParent);

                        task.ContinueWith(
                            tr =>
                                {
                                    using (var stream = tr.Result.GetResponseStream())
                                    {
                                        if (stream == null)
                                        {
                                            return;
                                        }

                                        using (var reader = new StreamReader(stream))
                                        {
                                            var json = reader.ReadToEnd();
                                            widgetInfo = JsonConvert.DeserializeObject<SoundCloudResponse>(json);
                                        }
                                    }
                                }, TaskContinuationOptions.AttachedToParent);
                    });

            parentTask.RunSynchronously();

            return new ContentProviderResultModel
            {
                Title = widgetInfo.Title,
                Content = widgetInfo.FrameMarkup
            };
        }

        protected override bool IsValidContent(HttpWebResponse response)
        {
            return response.ResponseUri.Host.IndexOf("soundcloud.com", StringComparison.OrdinalIgnoreCase) >= 0;
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