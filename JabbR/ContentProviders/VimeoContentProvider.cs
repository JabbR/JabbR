

using System;
using System.IO;
using System.Net;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Web;

namespace JabbR.ContentProviders
{
    public class VimeoContentProvider : EmbedContentProvider
    {
        protected override IEnumerable<object> ExtractParameters(Uri responseUri)
        {
            if (responseUri.Segments.Length > 1)
            {
                string videoId = responseUri.Segments[1];
                if (!String.IsNullOrEmpty(videoId))
                {
                    yield return videoId;
                }
            }
            yield return "";
        }


        public override System.Collections.Generic.IEnumerable<string> Domains
        {
            get { return new String[] { "http://vimeo.com", "http://www.vimeo.com" }; }
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