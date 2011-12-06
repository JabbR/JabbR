using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using Newtonsoft.Json;

namespace JabbR.ContentProviders
{
    public class UStreamContentProvider : CollapsibleContentProvider
    {
        private static readonly Regex _locateIdRegex = new Regex(@"(\d+)|.*/(channel|recorded)/(.+)");
        private static readonly string _channelEmbedFormat = @"<iframe src=""http://www.ustream.tv/embed/{0}"" width=""700"" height=""400"" scrolling=""no"" frameborder=""0"" style=""border: 0px none transparent;""></iframe>";
        private static readonly string _apiQueryFormat = @"http://api.ustream.tv/json/{0}/{1}/getInfo";

        protected override ContentProviderResultModel GetCollapsibleContent(HttpWebResponse response)
        {
            var assetParts = ExtractParameters(response.ResponseUri);
          
            string noun = assetParts.ElementAt(0) == "recorded" ? "video" : assetParts.ElementAt(0);
            string prefix = assetParts.ElementAt(0) == "recorded" ? "recorded/" : "";

            dynamic assetData = FetchAssetData(noun, assetParts.ElementAt(1));
            
            return new ContentProviderResultModel()
            {
                Content = String.Format(_channelEmbedFormat, prefix + assetData.results.id),
                Title = assetData.results.title
            };
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



        protected IEnumerable<string> ExtractParameters(Uri responseUri)
        {
            return _locateIdRegex.Match(responseUri.AbsoluteUri)
                                .Groups
                                .Cast<Group>()
                                .Skip(1)
                                .Select(g => g.Value)
                                .Where(v => !String.IsNullOrEmpty(v));

        }

        protected override bool IsValidContent(HttpWebResponse response)
        {
            return response.ResponseUri.AbsoluteUri.StartsWith("http://ustream.tv/", StringComparison.OrdinalIgnoreCase)
               || response.ResponseUri.AbsoluteUri.StartsWith("http://www.ustream.tv/", StringComparison.OrdinalIgnoreCase);
        }
    }
}