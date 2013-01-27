using System;
using JabbR.Infrastructure;
using JabbR.Models;
using JabbR.Services;
using JabbR.ViewModels;
using Nancy;
using Nancy.Cookies;
using WorldDomination.Web.Authentication;

namespace JabbR.Nancy
{
    public class AccountModule : JabbRModule
    {
        public AccountModule(IApplicationSettings applicationSettings,
                             IAuthenticationTokenService authenticationTokenService,
                             IMembershipService membershipService,
                             IJabbrRepository repository,
                             IAuthenticationService authService)
            : base("/account")
        {
            Get["/"] = _ =>
            {
                ChatUser user = repository.GetUserById(Context.CurrentUser.UserName);

                return View["index", new ProfilePageViewModel(user, authService.Providers)];
            };

            Get["/login"] = _ =>
            {
                return View["login", GetLoginViewModel(applicationSettings, repository, authService)];
            };

            Post["/login"] = param =>
            {
                string name = Request.Form.username;
                string password = Request.Form.password;

                if (String.IsNullOrEmpty(name))
                {
                    AddValidationError("name", "Name is required");
                }

                if (String.IsNullOrEmpty(password))
                {
                    AddValidationError("password", "Password is required");
                }

                try
                {
                    if (ModelValidationResult.IsValid)
                    {
                        ChatUser user = membershipService.AuthenticateUser(name, password);
                        return this.CompleteLogin(authenticationTokenService, user);
                    }
                    else
                    {
                        return View["login", GetLoginViewModel(applicationSettings, repository, authService)];
                    }
                }
                catch (Exception ex)
                {
                    AddValidationError("_FORM", ex.Message);
                    return View["login", GetLoginViewModel(applicationSettings, repository, authService)];
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

                if (String.IsNullOrEmpty(name))
                {
                    AddValidationError("name", "Name is required");
                }

                if (String.IsNullOrEmpty(email))
                {
                    AddValidationError("email", "Email is required");
                }

                if (String.IsNullOrEmpty(password))
                {
                    AddValidationError("password", "Password is required");
                }

                if (!String.Equals(password, confirmPassword))
                {
                    AddValidationError("confirmPassword", "Passwords don't match");
                }

                try
                {
                    if (ModelValidationResult.IsValid)
                    {
                        ChatUser user = membershipService.AddUser(name, email, password);
                        return this.CompleteLogin(authenticationTokenService, user);
                    }
                    else
                    {
                        return View["register", ModelValidationResult];
                    }
                }
                catch(Exception ex)
                {
                    AddValidationError("_FORM", ex.Message);
                    return View["register", ModelValidationResult];
                }
            };
        }

        private LoginViewModel GetLoginViewModel(IApplicationSettings applicationSettings, IJabbrRepository repository,
                                                 IAuthenticationService authService)
        {
            ChatUser user = null;

            if (Context.CurrentUser != null)
            {
                user = repository.GetUserById(Context.CurrentUser.UserName);
            }

            var viewModel = new LoginViewModel(applicationSettings.AuthenticationMode,
                                               authService.Providers,
                                               user != null ? user.Identities : null);
            return viewModel;
        }
    }
}