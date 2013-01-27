using System;
using JabbR.Infrastructure;
using JabbR.Models;
using JabbR.Services;
using Nancy;
using Nancy.Cookies;

namespace JabbR.Nancy
{
    public static class NancyExtensions
    {
        public static Response CompleteLogin(this NancyModule module,
                                             IAuthenticationTokenService authenticationTokenService,
                                             ChatUser user)
        {
            var response = module.Response.AsRedirect("~/");
            response.AddAuthenticationCookie(authenticationTokenService, user);
            return response;
        }

        private static void AddAuthenticationCookie(this Response response,
                                                   IAuthenticationTokenService authenticationTokenService,
                                                   ChatUser user)
        {
            string userToken = authenticationTokenService.GetAuthenticationToken(user);
            var cookie = new NancyCookie(Constants.UserTokenCookie, userToken, httpOnly: true)
            {
                Expires = DateTime.Now + TimeSpan.FromDays(30)
            };

            response.AddCookie(cookie);
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
    }
}