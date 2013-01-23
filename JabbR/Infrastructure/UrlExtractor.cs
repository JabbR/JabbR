using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace JabbR.Infrastructure
{
    public class UrlExtractor
    {
        private static Regex urlPattern = new Regex(@"(?i)\b((?:[a-z][\w-]+:(?:/{1,3}|[a-z0-9%])|www\d{0,3}[.]|[a-z0-9.\-]+[.][a-z]{2,4}/)(?:[^\s()<>]+|\(([^\s()<>]+|(\([^\s()<>]+\)))*\))+(?:\(([^\s()<>]+|(\([^\s()<>]+\)))*\)|[^\s`!()\[\]{};:'"".,<>?«»“”‘’]))", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public static IList<string> ExtractUrls(string message)
        {
            var urls = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var matches = urlPattern.Matches(message);

            foreach (Match m in matches)
            {
                string url = m.Value;
                if (!url.Contains("://"))
                {
                    url = "http://" + url;
                }

                if (Uri.IsWellFormedUriString(url, UriKind.Absolute))
                {
                    urls.Add(url);
                }
            };

            return urls.ToList();
        }
    }
}
