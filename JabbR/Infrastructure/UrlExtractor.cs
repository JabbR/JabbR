using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace JabbR.Infrastructure
{
    public class UrlExtractor
    {
        private static Regex urlPattern = new Regex(@"(?:(?:https?)://|www\.)[^\s]+", RegexOptions.Compiled | RegexOptions.IgnoreCase);

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
            }

            return urls.ToList();
        }
    }
}
