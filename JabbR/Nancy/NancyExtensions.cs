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
    }
}