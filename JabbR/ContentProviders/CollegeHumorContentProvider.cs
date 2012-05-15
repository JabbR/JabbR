using System.Collections.Generic;
using System.Text.RegularExpressions;
using JabbR.ContentProviders.Core;

namespace JabbR.ContentProviders
{
    public class CollegeHumorContentProvider : EmbedContentProvider
    {
        private static readonly Regex _videoIdRegex = new Regex(@".*video/(\d+).*");

        protected override Regex ParameterExtractionRegex
        {
            get
            {
                return _videoIdRegex;
            }
        }

        public override IEnumerable<string> Domains
        {
            get { yield return "http://www.collegehumor.com"; }
        }

        public override string MediaFormatString
        {
            get
            {
                return @"<object type=""application/x-shockwave-flash"" data=""http://www.collegehumor.com/moogaloop/moogaloop.swf?clip_id={0}&use_node_id=true&fullscreen=1"" width=""600"" height=""338""><param name=""allowfullscreen"" value=""true""/><param name=""wmode"" value=""transparent""/><param name=""allowScriptAccess"" value=""always""/><param name=""movie"" quality=""best"" value=""http://www.collegehumor.com/moogaloop/moogaloop.swf?clip_id={0}&use_node_id=true&fullscreen=1""/><embed src=""http://www.collegehumor.com/moogaloop/moogaloop.swf?clip_id={0}&use_node_id=true&fullscreen=1"" type=""application/x-shockwave-flash"" wmode=""transparent"" width=""600"" height=""338"" allowScriptAccess=""always""></embed></object>";
            }
        }
    }
}