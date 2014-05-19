using System;
using System.Collections.Generic;

namespace JabbR.Services
{
    public class ContentProviderSetting
    {
        public static List<ContentProviderSetting> GetDefaultContentProviders()
        {
            return new List<ContentProviderSetting>
            {
                new ContentProviderSetting
                {
                    Name = "gist",
                    Enabled = true,
                    Domains = "https://gist.github.com",
                    Extract = @"(\w+$)",
                    Script = "https://gist.github.com/{0}.js", // todo: does this need https?
                    Title = "https://gist.github.com/{0}"
                },
                new ContentProviderSetting
                {
                    Name = "tweet",
                    Enabled = true,
                    Collapsible = true,
                    Domains = "http://twitter.com/;https://twitter.com/;http://www.twitter.com/;https://www.twitter.com/",
                    Extract = @".*/(?:statuses|status)/(\d+)",
                    Output = "<div class='tweet_{0}'><script src='https://api.twitter.com/1/statuses/oembed.json?id={0}&amp;callback=addTweet'></script></div>",
                },
                new ContentProviderSetting
                {
                    Name = "twitpic",
                    Enabled = true,
                    Domains = "http://twitpic.com;http://www.twitpic.com",
                    Extract = @".*twitpic\.com\/(\w+)",
                    Output = "<a href=\"http://twitpic.com/{0}\"> <img src=\"http://twitpic.com/show/large/{0}\"></a>",
                    Collapsible = true,
                },
                new ContentProviderSetting
                {
                    Name = "pastie",
                    Enabled = true,
                    Domains = "http://pastie.org/;http://www.pastie.org/",
                    Extract = @"(\d+)",
                    Script = "http://pastie.org/{0}.js",
                    Title = "http://pastie.org/{0}",
                },
                new ContentProviderSetting
                {
                    Name = "vimeo",
                    Enabled = true,
                    Domains = "http://vimeo.com;http://www.vimeo.com",
                    Extract = @"(\d+)",
                    Output = "<iframe src=\"//player.vimeo.com/video/{0}?title=0&amp;byline=0&amp;portrait=0&amp;color=c9ff23\" width=\"500\" height=\"271\" frameborder=\"0\" webkitAllowFullScreen mozallowfullscreen allowFullScreen></iframe>",
                    Collapsible = true,
                },
                new ContentProviderSetting
                {
                    Name = "collegehumor",
                    Enabled = true,
                    Domains = "http://collegehumor.com;http://www.collegehumor.com",
                    Extract = @".*video/(\d+).*",
                    Output = "<object type=\"application/x-shockwave-flash\" data=\"http://www.collegehumor.com/moogaloop/moogaloop.swf?clip_id={0}&use_node_id=true&fullscreen=1\" width=\"600\" height=\"338\"><param name=\"allowfullscreen\" value=\"true\"/><param name=\"wmode\" value=\"transparent\"/><param name=\"allowScriptAccess\" value=\"always\"/><param name=\"movie\" quality=\"best\" value=\"http://www.collegehumor.com/moogaloop/moogaloop.swf?clip_id={0}&use_node_id=true&fullscreen=1\"/><embed src=\"http://www.collegehumor.com/moogaloop/moogaloop.swf?clip_id={0}&use_node_id=true&fullscreen=1\" type=\"application/x-shockwave-flash\" wmode=\"transparent\" width=\"600\" height=\"338\" allowScriptAccess=\"always\"></embed></object>",
                    Collapsible = true,
                },
                new ContentProviderSetting
                {
                    Name = "joinme",
                    Enabled = true,
                    Domains = "https://join.me/",
                    Extract = @"(.*)",
                    Output = "<iframe src=\"{0}\" width=\"700\" height=\"400\"></iframe>",
                    Collapsible = true,
                },
                new ContentProviderSetting
                {
                    Name = "mixcloud",
                    Enabled = true,
                    Domains = "http://www.mixcloud.com/",
                    Extract = @"(.*)",
                    Output = "<object width=\"100%\" height=\"120\"><param name=\"movie\" value=\"//www.mixcloud.com/media/swf/player/mixcloudLoader.swf?feed={0}&embed_uuid=06c5d381-1643-407d-80e7-2812382408e9&stylecolor=1e2671&embed_type=widget_standard\"></param><param name=\"allowFullScreen\" value=\"true\"></param><param name=\"wmode\" value=\"opaque\"></param><param name=\"allowscriptaccess\" value=\"always\"></param><embed src=\"//www.mixcloud.com/media/swf/player/mixcloudLoader.swf?feed={0}&embed_uuid=06c5d381-1643-407d-80e7-2812382408e9&stylecolor=1e2671&embed_type=widget_standard\" type=\"application/x-shockwave-flash\" wmode=\"opaque\" allowscriptaccess=\"always\" allowfullscreen=\"true\" width=\"100%\" height=\"120\"></embed></object>",
                    Collapsible = true,
                },
            };
        }

        public string Name { get; set; }
        public bool Enabled { get; set; }
        public bool Collapsible { get; set; }
        public string Domains { get; set; }
        public string Extract { get; set; }
        public string Output { get; set; }
        public string Script { get; set; }
        public string Title { get; set; }

        private IList<string> _domains;
        public IList<string> GetDomains()
        {
            if (_domains != null)
                return _domains;

            _domains = Domains.Split(new [] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            return _domains;
        }
    }
}