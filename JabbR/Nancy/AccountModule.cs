using System;
using JabbR.Infrastructure;
using JabbR.Models;
using JabbR.Services;
using Nancy;
using Nancy.Cookies;

namespace JabbR.Nancy
{
    public class AccountModule : NancyModule
    {
        public AccountModule(IApplicationSettings applicationSettings,
                             IAuthenticationService authService,
                             IMembershipService membershipService)
        {
            Get["/account/login"] = _ => View["login", applicationSettings.AuthenticationMode];

            Post["/account/login"] = param =>
            {
                string name = Request.Form.username;
                string password = Request.Form.password;

                try
                {
                    if (!String.IsNullOrEmpty(name) &&
                       !String.IsNullOrEmpty(password))
                    {
                        ChatUser user = membershipService.AuthenticateUser(name, password);
                        return CompleteLogin(authService, user);
                    }
                    else
                    {
                        return View["login", applicationSettings.AuthenticationMode];
                    }
                }
                catch
                {
                    return View["login", applicationSettings.AuthenticationMode];
                }
            };

            Post["/account/logout"] = _ =>
            {
                var response = Response.AsJson(new { success = true });

                response.AddCookie(new NancyCookie(Constants.UserTokenCookie, null)
                {
                    Expires = DateTime.Now.AddDays(-1)
                });

                return response;
            };

            Get["/account/register"] = _ => View["register"];

            Post["/account/create"] = _ =>
            {
                string name = Request.Form.username;
                string password = Request.Form.password;
                string confirmPassword = Request.Form.confirmPassword;

                try
                {
                    if (!String.Equals(password, confirmPassword))
                    {
                        return View["register"];
                    }

                    ChatUser user = membershipService.AddUser(name, password);
                    return CompleteLogin(authService, user);
                }
                catch
                {
                    return View["register"];
                }
            };
        }

        private Response CompleteLogin(IAuthenticationService authService, ChatUser user)
        {
            string userToken = authService.GetAuthenticationToken(user);
            var cookie = new NancyCookie(Constants.UserTokenCookie, userToken, httpOnly: true)
            {
                Expires = DateTime.Now + TimeSpan.FromDays(30)
            };

            var response = Response.AsRedirect("~/");
            response.AddCookie(cookie);
            return response;
        }
    }
}