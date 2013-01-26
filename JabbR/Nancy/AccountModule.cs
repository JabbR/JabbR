using System;
using JabbR.Infrastructure;
using JabbR.Models;
using JabbR.Services;
using JabbR.ViewModels;
using Nancy;
using Nancy.Cookies;

namespace JabbR.Nancy
{
    public class AccountModule : NancyModule
    {
        public AccountModule(IApplicationSettings applicationSettings,
                             IAuthenticationTokenService authenticationTokenService,
                             IMembershipService membershipService,
                             IJabbrRepository repository)
            : base("/account")
        {
            Get["/"] = _ =>
            {
                ChatUser user = repository.GetUserById(Context.CurrentUser.UserName);

                return View["index", new ProfilePageViewModel(user)];
            };

            Get["/login"] = _ => View["login", applicationSettings.AuthenticationMode];

            Post["/login"] = param =>
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

            Post["/logout"] = _ =>
            {
                var response = Response.AsJson(new { success = true });

                response.AddCookie(new NancyCookie(Constants.UserTokenCookie, null)
                {
                    Expires = DateTime.Now.AddDays(-1)
                });

                return response;
            };

            Get["/register"] = _ => View["register"];

            Post["/create"] = _ =>
            {
                string name = Request.Form.username;
                string email = Request.Form.email;
                string password = Request.Form.password;
                string confirmPassword = Request.Form.confirmPassword;

                try
                {
                    if (!String.Equals(password, confirmPassword))
                    {
                        return View["register"];
                    }

                    if (String.IsNullOrEmpty(email))
                    {
                        return View["register"];
                    }

                    ChatUser user = membershipService.AddUser(name, email, password);
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