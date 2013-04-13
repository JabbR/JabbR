using System;
using System.Collections.Generic;
using System.Security.Claims;
using JabbR.Infrastructure;
using JabbR.Models;
using Nancy;
using Nancy.Owin;
using Owin.Types;
using Owin.Types.Extensions;

namespace JabbR.Nancy
{
    public static class NancyExtensions
    {
        /// <summary>
        /// Sets the Auth Cookie and Redirects
        /// </summary>
        /// <param name="module"></param>
        /// 
        /// <param name="user">the User to be logged in</param>
        /// <param name="redirectUrl">optional URL to redirect to, default is querystring returnUrl, if present, otherwise the root</param>
        /// <returns></returns>
        public static Response SignIn(this NancyModule module, ChatUser user, string redirectUrl = null)
        {
            var env = Get<IDictionary<string, object>>(module.Context.Items, NancyOwinHost.RequestEnvironmentKey);
            var owinResponse = new OwinResponse(env);


            var claims = new List<Claim>();
            claims.Add(new Claim(ClaimTypes.NameIdentifier, user.Id));
            var identity = new ClaimsIdentity(claims, Constants.JabbRAuthType);
            owinResponse.SignIn(new ClaimsPrincipal(identity));

            string returnUrl = redirectUrl ?? module.Request.Query.return_Url;
            if (String.IsNullOrWhiteSpace(returnUrl))
            {
                returnUrl = "~/";
            }

            var response = module.Response.AsRedirect(returnUrl);
            return response;
        }

        public static void SignOut(this NancyModule module)
        {
            var env = Get<IDictionary<string, object>>(module.Context.Items, NancyOwinHost.RequestEnvironmentKey);
            var owinResponse = new OwinResponse(env);

            owinResponse.SignOut(Constants.JabbRAuthType);
        }

        public static void AddValidationError(this NancyModule module, string propertyName, string errorMessage)
        {
            module.ModelValidationResult = module.ModelValidationResult.AddError(propertyName, errorMessage);
        }

        public static void AddAlertMessage(this NancyModule module, string messageType, string alertMessage)
        {
            var container = module.Request.Session.GetSessionVaue<AlertMessageStore>(AlertMessageStore.AlertMessageKey);

            if (container == null)
            {
                container = new AlertMessageStore();
            }

            container.AddMessage(messageType, alertMessage);

            module.Request.Session.SetSessionValue(AlertMessageStore.AlertMessageKey, container);
        }

        public static ClaimsPrincipal GetPrincipal(this NancyModule module)
        {
            var userIdentity = module.Context.CurrentUser as ClaimsPrincipalUserIdentity;

            if (userIdentity == null)
            {
                return null;
            }

            return userIdentity.ClaimsPrincipal;
        }

        public static bool IsAuthenticated(this NancyModule module)
        {
            return module.GetPrincipal().IsAuthenticated();
        }

        private static T Get<T>(IDictionary<string, object> env, string key)
        {
            object value;
            if (env.TryGetValue(key, out value))
            {
                return (T)value;
            }
            return default(T);
        }
    }
}