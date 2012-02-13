using System;
using System.Linq;
using System.Text;
using System.Web;
using JabbR.Models;
using Newtonsoft.Json;
using System.Globalization;

namespace JabbR.Handlers
{
    public class DownloadHandler : IHttpHandler
    {
        const string FilenameDateFormat = "yyyy-MM-dd.HHmmsszz";

        IJabbrRepository _repository;

        public DownloadHandler(IJabbrRepository repository)
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

            switch(range)
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
                    throw new InvalidOperationException("Range value not recognized");
            }

            var room = _repository.VerifyRoom(roomName, mustBeOpen: false);

            if (room.Private)
            {
                throw new InvalidOperationException("Private room history download is not yet supported.");
            }

            var messages = _repository.GetMessagesByRoom(roomName)
                .Where(msg => msg.When <= end && msg.When >= start)
                .Select(msg => new DownloadMessage() { Content = msg.Content, Username = msg.User.Name, When = msg.When } );
            
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
                    var json = JsonConvert.SerializeObject(messages);
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
                    throw new NotSupportedException("Format value not recognized.");
            }
        }

        class DownloadMessage
        {
            public string Content { get; set; }
            public string Username { get; set; }
            public DateTimeOffset When { get; set; }
        }
    }
}