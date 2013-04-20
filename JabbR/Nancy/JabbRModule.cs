using System;
using System.Security.Claims;
using JabbR.Infrastructure;
using Nancy;

namespace JabbR.Nancy
{
    public class JabbRModule : NancyModule
    {
        public JabbRModule()
            : base()
        {
            Before.AddItemToEndOfPipeline(AlertsToViewBag);
            After.AddItemToEndOfPipeline(RemoveAlters);
        }

        public JabbRModule(string modulePath)
            : base(modulePath)
        {
            Before.AddItemToEndOfPipeline(AlertsToViewBag);
            After.AddItemToEndOfPipeline(RemoveAlters);
        }

        protected ClaimsPrincipal Principal
        {
            get { return this.GetPrincipal(); }
        }

        protected bool IsAuthenticated
        {
            get { return this.IsAuthenticated(); }
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

        internal static void RemoveAlters(NancyContext context)
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