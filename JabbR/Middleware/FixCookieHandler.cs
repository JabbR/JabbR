using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Owin;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.DataHandler;

namespace JabbR.Middleware
{
    using AppFunc = Func<IDictionary<string, object>, Task>;

    public class FixCookieHandler
    {
        private readonly AppFunc _next;
        private readonly TicketDataHandler _ticketHandler;

        public FixCookieHandler(AppFunc next, TicketDataHandler ticketHandler)
        {
            _ticketHandler = ticketHandler;
            _next = next;
        }

        public Task Invoke(IDictionary<string, object> env)
        {
            var request = new OwinRequest(env);

            var cookies = request.GetCookies();
            string cookieValue;
            if (cookies != null && cookies.TryGetValue("jabbr.id", out cookieValue))
            {
                AuthenticationTicket ticket = _ticketHandler.Unprotect(cookieValue);
                if (ticket.Extra == null)
                {
                    // The forms auth module has a bug where it null refs on a null Extra
                    var headers = request.Get<IDictionary<string, string[]>>(Owin.Types.OwinConstants.RequestHeaders);

                    var cookieBuilder = new StringBuilder();
                    foreach (var cookie in cookies)
                    {
                        // Skip the jabbr.id cookie
                        if (cookie.Key == "jabbr.id")
                        {
                            continue;
                        }

                        if (cookieBuilder.Length > 0)
                        {
                            cookieBuilder.Append(";");
                        }

                        cookieBuilder.Append(cookie.Key)
                                     .Append("=")
                                     .Append(Uri.EscapeDataString(cookie.Value));
                    }

                    headers["Cookie"] = new[] { cookieBuilder.ToString() };
                }
            }
            
            return _next(env);
        }
    }
}
