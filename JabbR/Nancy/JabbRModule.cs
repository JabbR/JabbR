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
            var item = context.Request.Session.GetSessionValue<AlertMessageStore>(AlertMessageStore.AlertMessageKey);

            var result = context.GetAuthenticationResult();

            if (result != null)
            {
                if (result.Success)
                {
                    item.AddMessage("success", result.Message);

                }
                else
                {
                    item.AddMessage("error", result.Message);
                }
            }

            context.ViewBag.Alerts = item;

            return null;
        }

        internal static void RemoveAlters(NancyContext context)
        {
            if (context.Response.StatusCode != HttpStatusCode.Unauthorized)
            {
                context.Request.Session.Delete(AlertMessageStore.AlertMessageKey);
            }
        }
    }
}