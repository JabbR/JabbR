using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using JabbR.ContentProviders.Core;

namespace JabbR.ContentProviders
{
    public class PastieContentProvider : EmbedContentProvider
    {
        private static readonly Regex _pasiteIdRegex = new Regex(@"(\d+)");

        public override IEnumerable<string> Domains
        {
            get
            {
                yield return "http://pastie.org/";
                yield return "http://www.pastie.org/";
            }
        }

        protected override Regex ParameterExtractionRegex
        {
            get
            {
                return _pasiteIdRegex;
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

        protected override bool IsCollapsible { get { return false; } }

        private const string ScriptTagFormat = @"
<div id='{0}'></div>
<script type='text/javascript'>
    captureDocumentWrite('http://pastie.org/{{0}}.js', 'http://pastie.org/{{0}}', $('#{0}'));
</script>";
    }
}