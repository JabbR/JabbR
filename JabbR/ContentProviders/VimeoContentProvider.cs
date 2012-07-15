using System.Collections.Generic;
using System.Text.RegularExpressions;
using JabbR.ContentProviders.Core;

namespace JabbR.ContentProviders
{
    public class VimeoContentProvider : EmbedContentProvider
    {
        private static readonly Regex _vimeoIdRegex = new Regex(@"(\d+)");

        protected override Regex ParameterExtractionRegex
        {
            get
            {
                return _vimeoIdRegex;
            }
        }


        public override IEnumerable<string> Domains
        {
            get
            {
                yield return "http://vimeo.com";
                yield return "http://www.vimeo.com";
            }
        }

        public override string MediaFormatString
        {
            get
            {
                return @"<iframe src=""//player.vimeo.com/video/{0}?title=0&amp;byline=0&amp;portrait=0&amp;color=c9ff23"" width=""500"" height=""271"" frameborder=""0"" webkitAllowFullScreen mozallowfullscreen allowFullScreen></iframe>";
            }
        }
    }
}