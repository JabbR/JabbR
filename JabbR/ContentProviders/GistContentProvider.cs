using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Web;

namespace JabbR.ContentProviders {
    public class GistContentProvider : EmbedContentProvider {
        private static readonly Regex _gistIdRegex = new Regex(@"(\d+)");
        
        public override IEnumerable<string> Domains 
        {
            get 
            {
                yield return "https://gist.github.com";
            }
        }

        public override Regex MediaUrlRegex {
            get {
                return _gistIdRegex;
            }
        }

        public override string MediaFormatString 
        {
            get 
            {
                var scriptTagId = Guid.NewGuid().ToString();
                return String.Format(ScriptTagFormat, scriptTagId);
            }
        }

        protected override string GetTitle(HttpWebResponse response) {
            return response.ResponseUri.ToString();
        }

        protected override bool IsCollapsible { get { return false; } }

        private string ScriptTagFormat {
            get {
                return @"
<div id='{0}'></div>
<script type='text/javascript'>
    captureDocumentWrite('https://gist.github.com/{{0}}.js', 'https://gist.github.com/{{0}}', $('#{0}'));
</script>
";
            }
        }
    }
}