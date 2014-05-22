using Nancy;
using Nancy.ErrorHandling;
using Nancy.ViewEngines;

namespace JabbR.Nancy
{
    public class ErrorPageHandler : DefaultViewRenderer, IStatusCodeHandler
    {
        public ErrorPageHandler(IViewFactory factory)
            : base(factory)
        {
        }

        public bool HandlesStatusCode(HttpStatusCode statusCode, NancyContext context)
        {
            // only handle 40x and 50x
            return (int)statusCode >= 400;
        }

        public void Handle(HttpStatusCode statusCode, NancyContext context)
        {
            var response = RenderView(
                context, 
                "errorPage", 
                new 
                { 
                    Error = statusCode,
                    ErrorCode = (int)statusCode 
                });

            response.StatusCode = statusCode;
            context.Response = response;
        }
    }
}