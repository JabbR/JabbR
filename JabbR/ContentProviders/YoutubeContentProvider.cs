using System;
using System.Collections.Generic;
using System.Web;
using JabbR.ContentProviders.Core;

namespace JabbR.ContentProviders
{
    public class YouTubeContentProvider : EmbedContentProvider
    {
        public override string MediaFormatString
        {
            get
            {
                return @"<object width=""425"" height=""344""><param name=""WMode"" value=""transparent""></param><param name=""movie"" value=""http://www.youtube.com/v/{0}fs=1""></param><param name=""allowFullScreen"" value=""true""></param><param name=""allowScriptAccess"" value=""always""></param><embed src=""http://www.youtube.com/v/{0}?fs=1"" wmode=""transparent"" type=""application/x-shockwave-flash"" allowfullscreen=""true"" allowscriptaccess=""always"" width=""425"" height=""344B""></embed></object>";
            }
        }

        public override IEnumerable<string> Domains
        {
            get
            {
                yield return "http://www.youtube.com";
            }
        }

        protected override IList<string> ExtractParameters(Uri responseUri)
        {
            var queryString = HttpUtility.ParseQueryString(responseUri.Query);
            string videoId = queryString["v"];
            
            if (!String.IsNullOrEmpty(videoId))
            {
                return new List<string>() { videoId };
            }
            return null;
        }
    }
}