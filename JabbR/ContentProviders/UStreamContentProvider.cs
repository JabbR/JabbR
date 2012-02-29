using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Web;
using JabbR.ContentProviders.Core;

namespace JabbR.ContentProviders
{
    public class UStreamContentProvider : CollapsibleContentProvider
    {
        private static readonly Regex _extractEmbedCodeRegex = new Regex(@"<textarea\s.*class=""embedCode"".*>(.*)</textarea>");

        protected override ContentProviderResultModel GetCollapsibleContent(Uri uri)
        {
            var response = MakeRequest(uri);
            var iframeHtml = HttpUtility.HtmlDecode(ExtractIFrameCode(response));
            return new ContentProviderResultModel()
            {
                Content = iframeHtml,
                Title = response.ResponseUri.AbsoluteUri.ToString()
            };
        }

        private string ExtractIFrameCode(HttpWebResponse response)
        {
            using (var responseStream = response.GetResponseStream())
            {
                using (var sr = new StreamReader(responseStream))
                {
                    var iframeStr = sr.ReadToEnd();

                    var matches = _extractEmbedCodeRegex.Match(iframeStr)
                                        .Groups
                                        .Cast<Group>()
                                        .Skip(1)
                                        .Select(g => g.Value)
                                        .Where(v => !String.IsNullOrEmpty(v));

                    return matches.FirstOrDefault() ?? String.Empty;
                }
            }
        }

        protected override bool IsValidContent(Uri uri)
        {
            return uri.AbsoluteUri.StartsWith("http://ustream.tv/", StringComparison.OrdinalIgnoreCase)
               || uri.AbsoluteUri.StartsWith("http://www.ustream.tv/", StringComparison.OrdinalIgnoreCase);
        }
    }
}