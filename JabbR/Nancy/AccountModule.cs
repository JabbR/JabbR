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
        public AccountModule(IAuthenticationService authService, IMembershipService membershipService)
        {
            Get["/account/login"] = _ => View["login"];

            Post["/account/login"] = param =>
            {
                string name = Request.Form.user;
                string password = Request.Form.password;

                var response = Response.AsRedirect("~/");

                try
                {
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
                    else
                    {
                        return View["login"];
                    }
                }
                catch
                {
                    return View["login"];
                }

                return response;
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
        }
    }
}