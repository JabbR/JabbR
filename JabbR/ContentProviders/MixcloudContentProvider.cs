using System;
using System.Collections.Generic;
using JabbR.ContentProviders.Core;

namespace JabbR.ContentProviders
{
    public class MixcloudContentProvider : EmbedContentProvider
    {
        public override string MediaFormatString
        {
            get
            {
                return "<object width=\"100%\" height=\"120\"><param name=\"movie\" value=\"//www.mixcloud.com/media/swf/player/mixcloudLoader.swf?feed={0}&embed_uuid=06c5d381-1643-407d-80e7-2812382408e9&stylecolor=1e2671&embed_type=widget_standard\"></param><param name=\"allowFullScreen\" value=\"true\"></param><param name=\"wmode\" value=\"opaque\"></param><param name=\"allowscriptaccess\" value=\"always\"></param><embed src=\"//www.mixcloud.com/media/swf/player/mixcloudLoader.swf?feed={0}&embed_uuid=06c5d381-1643-407d-80e7-2812382408e9&stylecolor=1e2671&embed_type=widget_standard\" type=\"application/x-shockwave-flash\" wmode=\"opaque\" allowscriptaccess=\"always\" allowfullscreen=\"true\" width=\"100%\" height=\"120\"></embed></object>";
            }
        }

        public override IEnumerable<string> Domains
        {
            get
            {
                yield return "http://www.mixcloud.com/";
            }
        }

        protected override IList<string> ExtractParameters(Uri responseUri)
        {
            return new List<string>() { responseUri.AbsoluteUri };
        }
    }
}