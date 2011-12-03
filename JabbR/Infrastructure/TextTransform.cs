using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using JabbR.Models;

namespace JabbR.Infrastructure {

    public class TextTransform {
        private readonly IJabbrRepository _repository;
        public const string HashTagPattern = @"(?:(?<=\s)|^)#(\w*[A-Za-z_]+\w*)";

        public TextTransform(IJabbrRepository repository) {
            this._repository = repository;
        }

        public string Parse(string message) {
            return ConvertHashtagsToRoomLinks(message);
        }

        private string ConvertHashtagsToRoomLinks(string message) {

            message = Regex.Replace(message, HashTagPattern, m => {
                string roomName = m.Groups[1].Value; /* hashtag without #*/

                if (_repository.GetRoomByName(roomName) != null) {
                    return string.Format(CultureInfo.InvariantCulture,
                                         "<a href=\"#/rooms/{0}\" title=\"{1}\">{1}</a>",
                                         roomName,
                                         m.Value /* full match */);
                }

                return m.Value;
            });

            return message;
        }

    }
}