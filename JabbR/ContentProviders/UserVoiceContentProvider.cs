using System;
using System.IO;
using System.Net;
using JabbR.ContentProviders.Core;
using Newtonsoft.Json;

namespace JabbR.ContentProviders {
    public class UserVoiceContentProvider : CollapsibleContentProvider {

        protected override ContentProviderResultModel GetCollapsibleContent(HttpWebResponse response) {
            var article = FetchArticle(response.ResponseUri);
            return new ContentProviderResultModel() {
                Title = article.title,
                Content = article.html
            };
        }

        private static dynamic FetchArticle(Uri url) {
            const string API = "http://{0}/api/v1/oembed.json?url={1}";
            var webRequest = (HttpWebRequest)WebRequest.Create(String.Format(API, url.Host, url.AbsoluteUri));
            webRequest.Accept = "application/json";
            using (var webResponse = webRequest.GetResponse()) {
                using (var sr = new StreamReader(webResponse.GetResponseStream())) {
                    return JsonConvert.DeserializeObject(sr.ReadToEnd());
                }
            }
        }

        protected override bool IsValidContent(HttpWebResponse response) {
            return response.ResponseUri.Host.IndexOf("uservoice.com", StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}