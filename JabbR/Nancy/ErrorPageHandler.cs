using System.Linq;

using JabbR.Services;

using Nancy;
using Nancy.ErrorHandling;
using Nancy.ViewEngines;

namespace JabbR.Nancy
{
    public class ErrorPageHandler : DefaultViewRenderer, IStatusCodeHandler
    {
        private readonly IJabbrRepository _repository;

        public ErrorPageHandler(IViewFactory factory, IJabbrRepository repository)
            : base(factory)
        {
            _repository = repository;
        }

        public bool HandlesStatusCode(HttpStatusCode statusCode, NancyContext context)
        {
            // only handle 40x and 50x
            return (int)statusCode >= 400;
        }

        public void Handle(HttpStatusCode statusCode, NancyContext context)
        {
            string suggestRoomName = null;
            if (statusCode == HttpStatusCode.NotFound && 
                context.Request.Url.Path.Count(e => e == '/') == 1)
            {
                // trim / from start of path
                var potentialRoomName = context.Request.Url.Path.Substring(1);
                if (_repository.GetRoomByName(potentialRoomName) != null)
                {
                    suggestRoomName = potentialRoomName;
                }
            }

            var response = RenderView(
                context, 
                "errorPage", 
                new 
                { 
                    Error = statusCode,
                    ErrorCode = (int)statusCode,
                    SuggestRoomName = suggestRoomName
                });

            response.StatusCode = statusCode;
            context.Response = response;
        }
    }
}