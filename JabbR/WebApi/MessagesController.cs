using JabbR.Infrastructure;
using JabbR.Models;
using JabbR.WebApi.Model;
using System;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace JabbR.WebApi
{
    public class MessagesController : ApiController
    {
        const string FilenameDateFormat = "yyyy-MM-dd.HHmmsszz";
        private IJabbrRepository _repository;

        public MessagesController(IJabbrRepository repository)
        {
            _repository = repository;
        }

        public HttpResponseMessage GetAllMessages(string room, string range)
        {
            if (String.IsNullOrWhiteSpace(range))
            {
                range = "last-hour";
            }

            var end = DateTime.Now;
            DateTime start;

            switch (range)
            {
                case "last-hour":
                    start = end.AddHours(-1);
                    break;
                case "last-day":
                    start = end.AddDays(-1);
                    break;
                case "last-week":
                    start = end.AddDays(-7);
                    break;
                case "last-month":
                    start = end.AddDays(-30);
                    break;
                case "all":
                    start = DateTime.MinValue;
                    break;
                default:
                    return Request.CreateJabbrErrorMessage(HttpStatusCode.BadRequest, "range value not recognized");
            }

            var filenamePrefix = room + ".";

            if (start != DateTime.MinValue)
            {
                filenamePrefix += start.ToString(FilenameDateFormat, CultureInfo.InvariantCulture) + ".";
            }

            filenamePrefix += end.ToString(FilenameDateFormat, CultureInfo.InvariantCulture);


            ChatRoom chatRoom = null;

            try
            {
                chatRoom = _repository.VerifyRoom(room, mustBeOpen: false);
            }
            catch (Exception ex)
            {
                return Request.CreateJabbrErrorMessage(HttpStatusCode.NotFound, ex.Message, filenamePrefix);
            }

            if (chatRoom.Private)
            {
                // TODO: Allow viewing messages using auth token
                return Request.CreateJabbrErrorMessage(HttpStatusCode.NotFound, String.Format("Unable to locate room {0}.", chatRoom.Name), filenamePrefix);
            }

            var messages = _repository.GetMessagesByRoom(chatRoom)
                .Where(msg => msg.When <= end && msg.When >= start)
                .OrderBy(msg => msg.When)
                .Select(msg => new MessageApiModel
                {
                    Content = msg.Content,
                    Username = msg.User.Name,
                    When = msg.When
                });


            return Request.CreateJabbrSuccessMessage(HttpStatusCode.OK, messages, filenamePrefix);
        }
    }
}