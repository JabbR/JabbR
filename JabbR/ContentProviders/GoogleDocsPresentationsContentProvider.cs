using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Web;
using JabbR.ContentProviders.Core;

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

        protected override bool IsValidContent(HttpWebResponse response)
        {
            if (!base.IsValidContent(response))
            {
                // Someone may have pasted a link requiring a login --
                // We can handle that here
                if (response.ResponseUri.AbsoluteUri.StartsWith("https://accounts.google.com/ServiceLogin") ||
                    response.ResponseUri.AbsoluteUri.StartsWith("http://accounts.google.com/ServiceLogin"))
                {
                    var qs = HttpUtility.ParseQueryString(response.ResponseUri.Query);
                    if (qs.AllKeys.Contains("continue"))
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