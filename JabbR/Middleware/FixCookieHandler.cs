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

            // The forms auth module has a bug where it null refs on a null Extra
            var headers = request.Get<IDictionary<string, string[]>>(Owin.Types.OwinConstants.RequestHeaders);

            var cookies = request.GetCookies();
            string cookieValue;
            if (cookies != null && cookies.TryGetValue("jabbr.id", out cookieValue))
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
                    foreach (var cookie in cookies)
                    {
                        string value = cookie.Value;

                        if (cookie.Key == "jabbr.id")
                        {
                            // Create a new ticket preserving the identity of the user
                            // so they don't get logged out
                            value = _ticketHandler.Protect(newTicket);
                            response.AddCookie("jabbr.id", value, new CookieOptions
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

                    headers["Cookie"] = new[] { cookieBuilder.ToString() };
                }
            }

            return _next(env);
        }
    }
}
