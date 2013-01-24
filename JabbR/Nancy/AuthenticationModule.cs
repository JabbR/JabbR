using System;
using JabbR.Infrastructure;
using JabbR.Models;
using JabbR.Services;
using Nancy;
using Nancy.Cookies;

namespace JabbR.Nancy
{
    public class AuthenticationModule : NancyModule
    {
        public AuthenticationModule(IAuthenticationService authService, IMembershipService membershipService)
        {
            Post["/"] = _ =>
            {
                string userToken;
                if (Request.Cookies.TryGetValue(Constants.UserTokenCookie, out userToken) &&
                    !String.IsNullOrEmpty(userToken) &&
                    authService.IsUserAuthenticated(userToken))
                {
                    return 200;
                }

                return 403;
            };

            Get["/login"] = _ => View["login"];

            Post["/login"] = param =>
            {
                string name = Request.Form.user;
                string password = Request.Form.password;

                var response = Response.AsRedirect("/");

                if (!String.IsNullOrEmpty(name) &&
                   !String.IsNullOrEmpty(password))
                {
                    ChatUser user = membershipService.AuthenticateUser(name, password);
                    string userToken = authService.GetAuthenticationToken(user);
                    var cookie = new NancyCookie(Constants.UserTokenCookie, userToken, httpOnly: true)
                    {
                        Expires = DateTime.Now + TimeSpan.FromDays(30)
                    };

                    response.AddCookie(cookie);
                }

                return response;
            };

            Post["/logout"] = _ =>
            {
                var response = Response.AsJson(new { success = true });

                response.AddCookie(new NancyCookie(Constants.UserTokenCookie, null)
                {
                    Expires = DateTime.Now.AddDays(-1)
                });

                return response;
            };
        }
    }
}