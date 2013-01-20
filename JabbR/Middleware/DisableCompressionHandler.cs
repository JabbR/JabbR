using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace JabbR.Middleware
{
    using AppFunc = Func<IDictionary<string, object>, Task>;

    /// <summary>
    /// Because we're letting the static file middlware serve static files we need
    /// to turn off IIS' gzipping so that the content length isn't totally messed up.
    /// This is bad as we won't get any gzipping but it's temporary until we find a
    /// better longer term solution.
    /// </summary>
    public class DisableCompressionHandler
    {
        private readonly AppFunc _next;

        public DisableCompressionHandler(AppFunc next)
        {
            _next = next;
        }

        public Task Invoke(IDictionary<string, object> env)
        {
            object callback;
            if (env.TryGetValue("systemweb.DisableResponseCompression", out callback) &&
                callback != null)
            {
                ((Action)callback)();
            }

            return _next(env);
        }
    }
}