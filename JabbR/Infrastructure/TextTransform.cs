using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using JabbR.Models;

namespace JabbR.Infrastructure
{

    public class TextTransform
    {
        private readonly IJabbrRepository _repository;
        public const string HashTagPattern = @"(?:(?<=\s)|^)#([A-Za-z0-9-_.]{1,30}\w*)";

        public TextTransform(IJabbrRepository repository)
        {
            _repository = repository;
        }

        public string Parse(string message)
        {
            return ConvertTextWithNewLines(ConvertHashtagsToRoomLinks(message));
        }

        private string ConvertTextWithNewLines(string message)
        {
            // If the message contains new lines wrap all of it in a pre tag
            if (message.Contains('\n'))
            {
                return String.Format(@"
<div class=""collapsible_content"">
    <h3 class=""collapsible_title"">Paste (click to show/hide)</h3>
    <div class=""collapsible_box"">
        <pre class=""multiline"">{0}</pre>
    </div>
</div>
", message);
            }

            return message;
        }

        public static string TransformAndExtractUrls(string message, out HashSet<string> extractedUrls)
        {
            const string urlPattern = @"((https?|ftp)://|www\.)[\w]+(.[\w]+)([\w\-\.\[\],@?^=%&amp;:/~\+#!]*[\w\-\@?^=%&amp;/~\+#\[\]])";

            var urls = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            message = Regex.Replace(message, urlPattern, m =>
            {
                string httpPortion = String.Empty;
                if (!m.Value.Contains("://"))
                {
                    httpPortion = "http://";
                }

                string url = httpPortion + m.Value;

                urls.Add(HttpUtility.HtmlDecode(url));

                return String.Format(CultureInfo.InvariantCulture,
                                     "<a rel=\"nofollow external\" target=\"_blank\" href=\"{0}\" title=\"{1}\">{1}</a>",
                                     url, m.Value);
            });

            extractedUrls = urls;
            return message;
        }

        public string ConvertHashtagsToRoomLinks(string message)
        {
            message = Regex.Replace(message, HashTagPattern, m =>
            {
                //hashtag without #
                string roomName = m.Groups[1].Value;

                var room = _repository.GetRoomByName(roomName);

                if (room != null)
                {
                    return String.Format(CultureInfo.InvariantCulture,
                                         "<a href=\"#/rooms/{0}\" title=\"{1}\">{1}</a>",
                                         roomName,
                                         m.Value);
                }

                return m.Value;
            });

            return message;
        }

    }
}