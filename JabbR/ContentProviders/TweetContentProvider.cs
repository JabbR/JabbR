using System;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Web;
using JabbR.ContentProviders.Core;
using System.Threading.Tasks;

namespace JabbR.ContentProviders
{
    /// <summary>
    /// Content Provider that provides the necessary functionality to show Tweets in the chat when someone pastes a
    /// Twitter link (eg. http://twitter.com/#!/DanTup/status/117940664771162112). Send feedback/issues to Danny Tuppeny
    /// (@DanTup on Twitter).
    /// </summary>
    public class TweetContentProvider : CollapsibleContentProvider
    {
        /// <summary>
        /// Regex for parsing the tweet ID out of the link.
        /// </summary>
        private static readonly Regex _tweetRegex = new Regex(@".*/(?:statuses|status)/(\d+)");

        /// <summary>
        /// The block of HTML/Script that is sent to the client to render the tweet text. The tweet ID will be passed
        /// in as {0}.
        /// </summary>
        private static readonly string tweetScript = String.Format( // Be aware: Nested string.format placeholder!
            "<div class=\"tweet_{{0}}\"><script src=\"{0}\"></script></div>",
            HttpUtility.HtmlEncode("https://api.twitter.com/1/statuses/show/{0}.json?include_entities=false&callback=addTweet")
        );

        protected override Task<ContentProviderResult> GetCollapsibleContent(ContentProviderHttpRequest request)
        {

            // Extract the status id from the URL.
            var status = _tweetRegex.Match(request.RequestUri.AbsoluteUri)
                                .Groups
                                .Cast<Group>()
                                .Skip(1)
                                .Select(g => g.Value)
                                .FirstOrDefault();

            // It's possible the link didn't have a status, so only process it if there was a match.
            if (!String.IsNullOrWhiteSpace(status))
            {
                return TaskAsyncHelper.FromResult(new ContentProviderResult()
                {
                    Content = String.Format(tweetScript, status),
                    Title = request.RequestUri.AbsoluteUri
                });
            }

            return TaskAsyncHelper.FromResult<ContentProviderResult>(null);
        }

        public override bool IsValidContent(Uri uri)
        {
            return uri.AbsoluteUri.StartsWith("http://twitter.com/", StringComparison.OrdinalIgnoreCase)
                || uri.AbsoluteUri.StartsWith("https://twitter.com/", StringComparison.OrdinalIgnoreCase)
                || uri.AbsoluteUri.StartsWith("http://www.twitter.com/", StringComparison.OrdinalIgnoreCase)
                || uri.AbsoluteUri.StartsWith("https://www.twitter.com/", StringComparison.OrdinalIgnoreCase);
        }
    }
}
