using System;
using System.Collections.Generic;
using System.Security.Principal;
using JabbR.Infrastructure;
using JabbR.Models;
using JabbR.Services;
using JabbR.ViewModels;
using Nancy;
using Nancy.Cookies;
using Nancy.Owin;

namespace JabbR.Nancy
{
    public class AccountModule : NancyModule
    {
        public AccountModule(IApplicationSettings applicationSettings,
                             IAuthenticationTokenService authenticationTokenService,
                             IMembershipService membershipService,
                             IJabbrRepository repository)
        {
            Get["/account"] = _ =>
                {
                    var user = repository.GetUserById(Context.CurrentUser.UserName);
                    return View["index", new UserViewModel(user)];
                };

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
                        return this.CompleteLogin(authenticationTokenService, user);
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
                    return this.CompleteLogin(authenticationTokenService, user);
                }
                catch
                {
                    return View["register"];
                }
            };
        }
    }
}