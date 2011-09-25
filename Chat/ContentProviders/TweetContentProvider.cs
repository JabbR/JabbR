using System;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Web;

namespace SignalR.Samples.Hubs.Chat.ContentProviders
{
	/// <summary>
	/// Content Provider that provides the necessary functionality to show Tweets in the chat when someone pastes a
	/// Twitter link (eg. http://twitter.com/#!/DanTup/status/117940664771162112). Send feedback/issues to Danny Tuppeny
	/// (@DanTup on Twitter).
	/// </summary>
	public class TweetContentProvider : IContentProvider
	{
		/// <summary>
		/// Regex for parsing the tweet ID out of the link.
		/// </summary>
		private static readonly Regex tweetRegex = new Regex(@".*http://twitter.com/(?:#!/)*[^/]+/status/(\d+).*");

		/// <summary>
		/// The block of HTML/Script that is sent to the client to render the tweet text. The tweet ID will be passed
		/// in as {0}.
		/// </summary>
		private static readonly string tweetScript = string.Format( // Be aware: Nested string.format placeholder!
			"<div class=\"tweet_{{0}}\"><script src=\"{0}\"></script></div>",
			HttpUtility.HtmlEncode("http://api.twitter.com/1/statuses/show/{0}.json?include_entities=false&callback=addTweet")
		);

		public string GetContent(HttpWebResponse response)
		{
			// Only process Twitter links (returning null means we didn't process the link).
			if (response.ResponseUri.AbsoluteUri.StartsWith("http://twitter.com/", StringComparison.OrdinalIgnoreCase))
			{
				// Extract the status id from the URL.
				var status = tweetRegex.Match(response.ResponseUri.AbsoluteUri)
									.Groups
									.Cast<Group>()
									.Skip(1)
									.Select(g => g.Value)
									.FirstOrDefault();

				// It's possible the link didn't have a status, so only process it if there was a match.
				if (!string.IsNullOrWhiteSpace(status))
				{
					return string.Format(tweetScript, status);
				}
			}

			return null;
		}
	}
}