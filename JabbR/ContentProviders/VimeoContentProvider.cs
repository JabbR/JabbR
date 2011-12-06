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
                return "<object width=\"500\" height=\"282  \">" +
                    "<param name=\"allowfullscreen\" value=\"true\" />" +
                    "<param name=\"allowscriptaccess\" value=\"always\" />" +
                    "<param name=\"movie\" value=\"http://vimeo.com/moogaloop.swf?clip_id={0}&amp;server=vimeo.com&amp;show_title=0&amp;show_byline=0&amp;show_portrait=0&amp;color=00adef&amp;fullscreen=1&amp;autoplay=0&amp;loop=0\" />" +
                    "<embed src=\"http://vimeo.com/moogaloop.swf?clip_id={0}&amp;server=vimeo.com&amp;show_title=0&amp;show_byline=0&amp;show_portrait=0&amp;color=00adef&amp;fullscreen=1&amp;autoplay=0&amp;loop=0\" " +
                    "type=\"application/x-shockwave-flash\" allowfullscreen=\"true\" allowscriptaccess=\"always\" width=\"500\" height=\"282\">" +
                    "</embed></object>";
            }
        }
    }
}