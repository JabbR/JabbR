using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using JabbR.ContentProviders.Core;
using JabbR.Infrastructure;

namespace JabbR.ContentProviders
{
    public class GoogleDocsPresentationsContentProvider : EmbedContentProvider
    {
        private static readonly Regex _googleDocsInternalUrlIdRegex = new Regex(@".*/d/(.+)/.|(?:id=)([a-zA-Z0-9-]*)");
        public override string MediaFormatString
        {
            get
            {
                return @"<iframe src='https://docs.google.com/presentation/embed?id={0}&start=false&loop=false&delayms=3000' frameborder='0' width='480' height='389' allowfullscreen='true' webkitallowfullscreen='true'></iframe>";
            }
        }

        protected override Regex ParameterExtractionRegex
        {
            get
            {
                return _googleDocsInternalUrlIdRegex;
            }
        }

        public override IEnumerable<string> Domains
        {
            get
            {
                yield return "https://docs.google.com/presentation";
                yield return "http://docs.google.com/presentation";
            }
        }

        public override bool IsValidContent(Uri uri)
        {
            if (!base.IsValidContent(uri))
            {
                // Someone may have pasted a link requiring a login --
                // We can handle that here
                if (uri.AbsoluteUri.StartsWith("https://accounts.google.com/ServiceLogin") ||
                    uri.AbsoluteUri.StartsWith("http://accounts.google.com/ServiceLogin"))
                {
                    var qs = new QueryStringCollection(uri.Query);
                    if (qs["continue"] != null)
                    {
                        return Domains.Any(d => qs["continue"].StartsWith(d));
                    }
                }
                return false;
            }
            return true;
        }
    }
}