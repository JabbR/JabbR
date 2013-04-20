using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using JabbR.Infrastructure;
using Ninject;
using Owin.Types;

namespace JabbR.Middleware
{
    using AppFunc = Func<IDictionary<string, object>, Task>;

    public class LoggingHandler
    {
        private readonly AppFunc _next;
        private readonly IKernel _kernel;

        public LoggingHandler(AppFunc next, IKernel kernel)
        {
            _next = next;
            _kernel = kernel;
        }

        public Task Invoke(IDictionary<string, object> env)
        {
            var request = new OwinRequest(env);

            var logger = _kernel.Get<ILogger>();

            var requestHeaders = new StringBuilder();
            foreach (var header in request.Headers)
            {
                requestHeaders.AppendLine(header.Key + " = " + request.GetHeader(header.Key));
            }

            Task task = _next(env);

            var response = new OwinResponse(env);

            var responseHeaders = new StringBuilder();
            foreach (var header in response.Headers)
            {
                responseHeaders.AppendLine(header.Key + " = " + response.GetHeader(header.Key));
            }

            logger.Log("URI: " + request.Uri + " " + Environment.NewLine +
                       "Request Headers: \r\n " + requestHeaders.ToString() + Environment.NewLine +
                       "Response Headers: \r\n " + responseHeaders.ToString());

            return task;
        }
    }
}