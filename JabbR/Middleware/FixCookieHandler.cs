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
            var response = new OwinResponse(env);

            string cookieValue = request.Cookies["jabbr.id"];
            if (!String.IsNullOrEmpty(cookieValue))
            {
                AuthenticationTicket ticket = _ticketHandler.Unprotect(cookieValue);
                if (ticket != null && ticket.Extra == null)
                {
                    var extra = new AuthenticationExtra();
                    extra.IsPersistent = true;
                    extra.IssuedUtc = DateTime.UtcNow;
                    extra.ExpiresUtc = DateTime.UtcNow.AddDays(30);

                    var newTicket = new AuthenticationTicket(ticket.Identity, extra);

                    var cookieBuilder = new StringBuilder();
                    foreach (var cookie in request.Cookies)
                    {
                        string value = cookie.Value;

                        if (cookie.Key == "jabbr.id")
                        {
                            // Create a new ticket preserving the identity of the user
                            // so they don't get logged out
                            value = _ticketHandler.Protect(newTicket);
                            response.Cookies.Append("jabbr.id", value, new CookieOptions
                            {
                                Expires = extra.ExpiresUtc.Value.UtcDateTime,
                                HttpOnly = true
                            });
                        }

                        if (cookieBuilder.Length > 0)
                        {
                            cookieBuilder.Append(";");
                        }

                        cookieBuilder.Append(cookie.Key)
                                     .Append("=")
                                     .Append(Uri.EscapeDataString(value));
                    }

                    request.Headers["Cookie"] = cookieBuilder.ToString();
                }
            }

            return _next(env);
        }
    }
}
