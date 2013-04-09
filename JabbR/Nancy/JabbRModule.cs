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
        }

        public JabbRModule(string modulePath)
            : base(modulePath)
        {
            Before.AddItemToEndOfPipeline(AlertsToViewBag);
        }

        protected ClaimsPrincipal Principal
        {
            get
            {
                var userIdentity = Context.CurrentUser as ClaimsPrincipalUserIdentity;
                if (userIdentity == null)
                {
                    return null;
                }

                return userIdentity.ClaimsPrincipal;
            }
        }

        protected bool IsAuthenticated
        {
            get
            {
                return Principal.IsAuthenticated();
            }
        }

        internal static Response AlertsToViewBag(NancyContext context)
        {
            var item = context.Request.Session.GetSessionVaue<AlertMessageStore>(AlertMessageStore.AlertMessageKey);
            context.ViewBag.Alerts = item;

            context.Request.Session.Delete(AlertMessageStore.AlertMessageKey);

            return null;
        }
    }
}