using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using JabbR.ContentProviders.Core;

namespace JabbR.ContentProviders
{
    public class YouTubeContentProvider : EmbedContentProvider
    {
        // Regex taken from this SO answer: http://stackoverflow.com/a/5831191
        private static readonly Regex YoutubeRegex = new Regex(
            @"# Match non-linked youtube URL in the wild. (Rev:20111012)
            https?://         # Required scheme. Either http or https.
            (?:[0-9A-Z-]+\.)? # Optional subdomain.
            (?:               # Group host alternatives.
              youtu\.be/      # Either youtu.be,
            | youtube\.com    # or youtube.com followed by
              \S*             # Allow anything up to VIDEO_ID,
              [^\w\-\s]       # but char before ID is non-ID char.
            )                 # End host alternatives.
            ([\w\-]{11})      # $1: VIDEO_ID is exactly 11 chars.
            (?=[^\w\-]|$)     # Assert next char is non-ID or EOS.
            (?!               # Assert URL is not pre-linked.
              [?=&+%\w]*      # Allow URL (query) remainder.
              (?:             # Group pre-linked alternatives.
                [\'""][^<>]*> # Either inside a start tag,
              | </a>          # or inside <a> element text contents.
              )               # End recognized pre-linked alts.
            )                 # End negative lookahead assertion.
            [?=&+%\w-]*       # Consume any URL (query) remainder.",
            RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace);

        public override string MediaFormatString
        {
            get
            {
                return @"<object width=""425"" height=""344""><param name=""WMode"" value=""transparent""></param><param name=""movie"" value=""https://www.youtube.com/v/{0}fs=1""></param><param name=""allowFullScreen"" value=""true""></param><param name=""allowScriptAccess"" value=""always""></param><embed src=""https://www.youtube.com/v/{0}?fs=1"" wmode=""transparent"" type=""application/x-shockwave-flash"" allowfullscreen=""true"" allowscriptaccess=""always"" width=""425"" height=""344B""></embed></object>";
            }
        }

        public override IEnumerable<string> Domains
        {
            get
            {
                yield return "http://www.youtube.com";
                yield return "https://www.youtube.com";
                yield return "http://youtu.be";
                yield return "https://youtu.be";
            }
        }

        protected override IList<string> ExtractParameters(Uri responseUri)
        {
            Match match = YoutubeRegex.Match(responseUri.ToString());
            if (match.Groups.Count < 2 || String.IsNullOrEmpty(match.Groups[1].Value))
            {
                return null;
            }

            string videoId = match.Groups[1].Value;
            return new List<string> { videoId };
        }
    }
}
