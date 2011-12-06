using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using JabbR.ContentProviders.Core;
using System.Web;

namespace JabbR.ContentProviders
{
    public class UStreamContentProvider : CollapsibleContentProvider
    {
        private static readonly string _apiQueryFormat = @"http://api.ustream.tv/json/{0}/{1}/getInfo";
        private static readonly Regex _extractEmbedCodeRegex = new Regex(@"<textarea\s.*class=""embedCode"".*>(.*)</textarea>");

        protected override ContentProviderResultModel GetCollapsibleContent(HttpWebResponse response)
        {
            var iframeHtml = HttpUtility.HtmlDecode(ExtractIFrameCode(response));
            return new ContentProviderResultModel()
            {
                Content = iframeHtml,
                Title = response.ResponseUri.AbsoluteUri.ToString()
            };
        }

        private string ExtractIFrameCode(HttpWebResponse response)
        {
            var iframeStr = new System.IO.StreamReader(response.GetResponseStream()).ReadToEnd();

            var matches = _extractEmbedCodeRegex.Match(iframeStr)
                                .Groups
                                .Cast<Group>()
                                .Skip(1)
                                .Select(g => g.Value)
                                .Where(v => !String.IsNullOrEmpty(v));

            return matches.Count() > 0 ? matches.First() : string.Empty;
        }



        private dynamic FetchAssetData(string assetType, string embedId)
        {
            var webRequest = (HttpWebRequest)HttpWebRequest.Create(
                   String.Format(_apiQueryFormat, assetType, embedId));

            using (var webResponse = webRequest.GetResponse())
            {
                using (var reader = new StreamReader(webResponse.GetResponseStream()))
                {
                    dynamic ustreamAssetData = JsonConvert.DeserializeObject(reader.ReadToEnd());
                    return ustreamAssetData;
                }
            }
        }


        protected override bool IsValidContent(HttpWebResponse response)
        {
            return response.ResponseUri.AbsoluteUri.StartsWith("http://ustream.tv/", StringComparison.OrdinalIgnoreCase)
               || response.ResponseUri.AbsoluteUri.StartsWith("http://www.ustream.tv/", StringComparison.OrdinalIgnoreCase);
        }
    }
}