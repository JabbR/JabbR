using System;
using System.IO;
using System.Net;
using System.Linq;
using System.Text.RegularExpressions;
using JabbR.ContentProviders.Core;
using JabbR.Infrastructure;

namespace JabbR.ContentProviders
{
    public class SoundCloudContentProvider : CollapsibleContentProvider
    {

        private static readonly Regex _titleRegex = new Regex("<meta.*content=\"(.*)\".*property=\"og:title\".*/>");
        private static readonly Regex _trackIDExtractRegex = new Regex("<meta.*content=\"http://player\\.soundcloud\\.com/player\\.swf.*tracks%2F(.*?)&amp;.*property=\"og:video\"\\s*/>");

        protected override ContentProviderResultModel GetCollapsibleContent(HttpWebResponse response)
        {
            using (var sr = new StreamReader(response.GetResponseStream()))
            {
                var pageContent = sr.ReadToEnd();
                if (String.IsNullOrEmpty(pageContent))
                {
                    return null;
                }

                var trackID = _trackIDExtractRegex.FindMatches(pageContent).SingleOrDefault();
                var titleContent = _titleRegex.FindMatches(pageContent).SingleOrDefault();

                if (trackID == null || titleContent == null)
                {
                    return null;
                }

                return new ContentProviderResultModel() {
                    Title = titleContent,
                    Content = String.Format(@"<iframe width=""100%"" height=""166"" scrolling=""no"" frameborder=""no"" src=""http://w.soundcloud.com/player/?url=http%3A%2F%2Fapi.soundcloud.com%2Ftracks%2F{0}&show_artwork=true&amp;autoplay=false&ampshow_comments=false&ampcolor=00103F""></iframe>", trackID)
                };
            }
        }

        protected override bool IsValidContent(HttpWebResponse response)
        {
            return response.ResponseUri.Host.IndexOf("soundcloud.com", StringComparison.OrdinalIgnoreCase) >= 0;
        }

    }
}