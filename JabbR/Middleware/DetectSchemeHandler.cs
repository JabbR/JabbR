using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Owin;

namespace JabbR.Middleware
{
    using AppFunc = Func<IDictionary<string, object>, Task>;

    public class DetectSchemeHandler
    {
        private readonly AppFunc _next;

        public DetectSchemeHandler(AppFunc next)
        {
            _next = next;
        }

        public Task Invoke(IDictionary<string, object> env)
        {
            var request = new OwinRequest(env);

            // This header is set on app harbor since ssl is terminated at the load balancer
            var scheme = request.Headers.Get("X-Forwarded-Proto");

            if (!String.IsNullOrEmpty(scheme))
            {
                request.Scheme = scheme;
            }

            return _next(env);
        }
    }
}