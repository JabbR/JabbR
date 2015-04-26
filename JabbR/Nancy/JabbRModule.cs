using System;
using System.Globalization;
using System.Linq;
using System.Security.Claims;
using JabbR.Infrastructure;
using Nancy;
using Nancy.Security;

namespace JabbR.Nancy
{
    public class JabbRModule : NancyModule
    {
        public JabbRModule()
            : base()
        {
            Before.AddItemToEndOfPipeline(AlertsToViewBag);
            After.AddItemToEndOfPipeline(RemoveAlerts);
        }

        public JabbRModule(string modulePath)
            : base(modulePath)
        {
            Before.AddItemToEndOfPipeline(AlertsToViewBag);
            After.AddItemToEndOfPipeline(RemoveAlerts);
        }

        protected ClaimsPrincipal Principal
        {
            get { return this.GetPrincipal(); }
        }

        protected bool IsAuthenticated
        {
            get { return this.IsAuthenticated(); }
        }

        protected bool HasValidCsrfTokenOrSecHeader
        {
            get
            {
                // if we have no useragent or signalr in the useragent, then we don't worry about CSRF because it's not a browser.
                if (string.IsNullOrEmpty(Request.Headers.UserAgent)
                    || Request.Headers.UserAgent.IndexOf("signalr", StringComparison.InvariantCultureIgnoreCase) >= 0)
                {
                    return true;
                }

                if (Request.Headers["sec-jabbr-client"].FirstOrDefault() != null)
                {
                    return true;
                }

                try
                {
                    this.ValidateCsrfToken();
                }
                catch (CsrfValidationException)
                {
                    return false;
                }

                return true;
            }
        }

        internal static Response AlertsToViewBag(NancyContext context)
        {
            var result = context.GetAuthenticationResult();

            if (result != null)
            {
                if (!String.IsNullOrEmpty(result.Message))
                {
                    if (result.Success)
                    {
                        context.Request.AddAlertMessage("success", result.Message);

                    }
                    else
                    {
                        context.Request.AddAlertMessage("error", result.Message);
                    }
                }
            }

            var item = context.Request.Session.GetSessionValue<AlertMessageStore>(AlertMessageStore.AlertMessageKey);

            context.ViewBag.Alerts = item;

            return null;
        }

        internal static void RemoveAlerts(NancyContext context)
        {
            if (context.Response.StatusCode != HttpStatusCode.Unauthorized &&
                context.Response.StatusCode != HttpStatusCode.SeeOther &&
                context.Response.StatusCode != HttpStatusCode.Found)
            {
                context.Request.Session.Delete(AlertMessageStore.AlertMessageKey);
                context.Response.AddCookie(Constants.AuthResultCookie, null, DateTime.Now.AddDays(-1));
            }
        }
    }
}