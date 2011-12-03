using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using JabbR.Models;

namespace JabbR.Infrastructure {

    public class TextTransform 
    {
        private readonly IJabbrRepository _repository;
        public const string HashTagPattern = @"(?:(?<=\s)|^)#([A-Za-z0-9-_.]{1,30}\w*)";
        //public const string HashTagPattern = @"(?:(?<=\s)|^)#(\w*[A-Za-z_-]+\w*)";

        public TextTransform(IJabbrRepository repository) 
        {
            _repository = repository;
        }

        public string Parse(string message) 
        {
            return ConvertHashtagsToRoomLinks(message);
        }

        private string ConvertHashtagsToRoomLinks(string message) 
        {

            message = Regex.Replace(message, HashTagPattern, m => {
                //hashtag without #
                string roomName = m.Groups[1].Value;

                var room = _repository.GetRoomByName(roomName);

                if (room != null) {
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