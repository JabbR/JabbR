using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Owin;

namespace JabbR.Middleware
{
    using Microsoft.Owin.Security;
    using Microsoft.Owin.Security.DataHandler;
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
            }
            
            return _next(env);
        }
    }
}
