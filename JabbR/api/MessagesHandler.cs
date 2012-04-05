using System;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Web;
using JabbR.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace JabbR.Handlers
{
    public class MessagesHandler : IHttpHandler
    {
        const string FilenameDateFormat = "yyyy-MM-dd.HHmmsszz";

        IJabbrRepository _repository;

        public MessagesHandler(IJabbrRepository repository)
        {
            _repository = repository;
        }

        public bool IsReusable
        {
            get { return false; }
        }

        public void ProcessRequest(HttpContext context)
        {
            var request = context.Request;
            var response = context.Response;
            var routeData = request.RequestContext.RouteData.Values;

            var roomName = (string)routeData["room"];
            var formatName = (string)routeData["format"];
            var range = request["range"];

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
                    WriteBadRequest(response, "range value not recognized");
                    return;
            }

            ChatRoom room = null;

            try
            {
                room = _repository.VerifyRoom(roomName, mustBeOpen: false);
            }
            catch (Exception ex)
            {
                WriteNotFound(response, ex.Message);
                return;
            }

            if (room.Private)
            {
                // TODO: Allow viewing messages using auth token
                WriteNotFound(response, String.Format("Unable to locate room {0}.", room.Name));
                return;
            }

            var messages = _repository.GetMessagesByRoom(roomName)
                .Where(msg => msg.When <= end && msg.When >= start)
                .OrderBy(msg => msg.When)
                .Select(msg => new
                {
                    Content = msg.Content,
                    Username = msg.User.Name,
                    When = msg.When
                });

            bool downloadFile = false;
            Boolean.TryParse(request["download"], out downloadFile);

            var filenamePrefix = roomName + ".";

            if (start != DateTime.MinValue)
            {
                filenamePrefix += start.ToString(FilenameDateFormat, CultureInfo.InvariantCulture) + ".";
            }

            filenamePrefix += end.ToString(FilenameDateFormat, CultureInfo.InvariantCulture);

            switch (formatName)
            {
                case "json":
                    var json = Serialize(messages);
                    var data = Encoding.UTF8.GetBytes(json);

                    response.ContentType = "application/json";
                    response.ContentEncoding = Encoding.UTF8;

                    if (downloadFile)
                    {
                        response.Headers["Content-Disposition"] = "attachment; filename=\"" + filenamePrefix + ".json\"";
                    }

                    response.BinaryWrite(data);
                    break;
                default:
                    WriteBadRequest(response, "format not supported.");
                    return;
            }
        }

        private void WriteBadRequest(HttpResponse response, string message)
        {
            WriteError(response, 400, "Bad request", message);
        }

        private void WriteNotFound(HttpResponse response, string message)
        {
            WriteError(response, 404, "Not found", message);
        }

        private void WriteError(HttpResponse response, int statusCode, string description, string message)
        {
            response.TrySkipIisCustomErrors = true;
            response.StatusCode = statusCode;
            response.StatusDescription = description;
            response.Write(Serialize(new ClientError { Message = message }));
        }

        private string Serialize(object value)
        {
            var resolver = new CamelCasePropertyNamesContractResolver();
            var settings = new JsonSerializerSettings
            {
                ContractResolver = resolver
            };

            settings.Converters.Add(new IsoDateTimeConverter());

            return JsonConvert.SerializeObject(value, Formatting.Indented, settings);
        }

        private class ClientError
        {
            public string Message { get; set; }
        }
    }
}