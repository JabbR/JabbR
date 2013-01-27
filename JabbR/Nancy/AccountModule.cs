using System;
using System.Linq;
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
                if (Context.CurrentUser == null)
                {
                    return HttpStatusCode.Forbidden;
                }

                ChatUser user = repository.GetUserById(Context.CurrentUser.UserName);

                return View["index", new ProfilePageViewModel(user, authService.Providers)];
            };

            Get["/login"] = _ =>
            {
                if (Context.CurrentUser != null)
                {
                    return Response.AsRedirect("~/");
                }

                return View["login", GetLoginViewModel(applicationSettings, repository, authService)];
            };

            Post["/login"] = param =>
            { 
                if (Context.CurrentUser != null)
                {
                    return Response.AsRedirect("~/");
                }

                string name = Request.Form.username;
                string password = Request.Form.password;

                if (String.IsNullOrEmpty(name))
                {
                    this.AddValidationError("name", "Name is required");
                }

                if (String.IsNullOrEmpty(password))
                {
                    this.AddValidationError("password", "Password is required");
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
                    this.AddValidationError("_FORM", ex.Message);
                    return View["login", GetLoginViewModel(applicationSettings, repository, authService)];
                }
            };

            Post["/logout"] = _ =>
            {
                if (Context.CurrentUser == null)
                {
                    return HttpStatusCode.Forbidden;
                }

                var response = Response.AsJson(new { success = true });

                response.AddCookie(new NancyCookie(Constants.UserTokenCookie, null)
                {
                    Expires = DateTime.Now.AddDays(-1)
                });

                return response;
            };

            Get["/register"] = _ =>
            {
                if (Context.CurrentUser != null)
                {
                    return Response.AsRedirect("~/");
                }

                return View["register"];
            };

            Post["/create"] = _ =>
            {
                if (Context.CurrentUser != null)
                {
                    return Response.AsRedirect("~/");
                }

                string name = Request.Form.username;
                string email = Request.Form.email;
                string password = Request.Form.password;
                string confirmPassword = Request.Form.confirmPassword;

                if (String.IsNullOrEmpty(name))
                {
                    this.AddValidationError("name", "Name is required");
                }

                if (String.IsNullOrEmpty(email))
                {
                    this.AddValidationError("email", "Email is required");
                }

                if (String.IsNullOrEmpty(password))
                {
                    this.AddValidationError("password", "Password is required");
                }

                if (!String.Equals(password, confirmPassword))
                {
                    this.AddValidationError("confirmPassword", "Passwords don't match");
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
                    this.AddValidationError("_FORM", ex.Message);
                    return View["register", ModelValidationResult];
                }
            };

            Post["/unlink"] = param =>
            {
                if (Context.CurrentUser == null)
                {
                    return HttpStatusCode.Forbidden;
                }

                string provider = Request.Form.provider;
                ChatUser user = repository.GetUserById(Context.CurrentUser.UserName);

                if (user.Identities.Count == 1 && !user.HasUserNameAndPasswordCredentials())
                {
                    this.AddAlertMessage("error", "You cannot unlink this provider because you would lose your ability to login.");
                    return Response.AsRedirect("~/account");
                }

                var identity = user.Identities.FirstOrDefault(i => i.ProviderName == provider);

                if (identity != null)
                {
                    repository.Remove(identity);

                    this.AddAlertMessage("success", String.Format("Successfully unlinked {0}", provider));
                    return Response.AsRedirect("~/account");
                }

                return HttpStatusCode.BadRequest;
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