using System;
using System.Linq;
using System.Security.Principal;
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
                             IAuthenticationService authService,
                             IChatNotificationService notificationService)
            : base("/account")
        {
            Get["/"] = _ =>
            {
                if (Context.CurrentUser == null)
                {
                    return HttpStatusCode.Forbidden;
                }

                ChatUser user = repository.GetUserById(Context.CurrentUser.UserName);

                return GetProfileView(authService, user);
            };

            Get["/login"] = _ =>
            {
                if (Context.CurrentUser != null)
                {
                    return Response.AsRedirect("~/");
                }

                var windowsPrincipal = Context.Items["windows.User"] as WindowsPrincipal;

                if (windowsPrincipal != null && 
                    windowsPrincipal.Identity.IsAuthenticated)
                {
                    // Detect windows authentication and automatically create a user or lookup a user
                    // based on the identity
                    ChatUser user = repository.GetUserById(windowsPrincipal.Identity.Name) ?? 
                                    membershipService.AddUser(windowsPrincipal);
                    return this.CompleteLogin(authenticationTokenService, user);
                }

                return View["login", GetLoginViewModel(applicationSettings, repository, authService)];
            };

            Post["/login"] = param =>
            {
                if (Context.CurrentUser != null)
                {
                    return Response.AsRedirect("~/");
                }

                string username = Request.Form.username;
                string password = Request.Form.password;

                if (String.IsNullOrEmpty(username))
                {
                    this.AddValidationError("username", "Name is required");
                }

                if (String.IsNullOrEmpty(password))
                {
                    this.AddValidationError("password", "Password is required");
                }

                try
                {
                    if (ModelValidationResult.IsValid)
                    {
                        ChatUser user = membershipService.AuthenticateUser(username, password);
                        return this.CompleteLogin(authenticationTokenService, user);
                    }
                    else
                    {
                        return View["login", GetLoginViewModel(applicationSettings, repository, authService)];
                    }
                }
                catch
                {
                    this.AddValidationError("_FORM", "Login failed. Check your username/password.");
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

                string username = Request.Form.username;
                string email = Request.Form.email;
                string password = Request.Form.password;
                string confirmPassword = Request.Form.confirmPassword;

                if (String.IsNullOrEmpty(username))
                {
                    this.AddValidationError("username", "Name is required");
                }

                if (String.IsNullOrEmpty(email))
                {
                    this.AddValidationError("email", "Email is required");
                }

                ValidatePassword(password, confirmPassword);

                try
                {
                    if (ModelValidationResult.IsValid)
                    {
                        ChatUser user = membershipService.AddUser(username, email, password);
                        return this.CompleteLogin(authenticationTokenService, user);
                    }
                }
                catch (Exception ex)
                {
                    this.AddValidationError("_FORM", ex.Message);
                }

                return View["register"];
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
                    this.AddAlertMessage("error", "You cannot unlink this account because you would lose your ability to login.");
                    return Response.AsRedirect("~/account/#identityProviders");
                }

                var identity = user.Identities.FirstOrDefault(i => i.ProviderName == provider);

                if (identity != null)
                {
                    repository.Remove(identity);

                    this.AddAlertMessage("success", String.Format("Successfully unlinked {0} account.", provider));
                    return Response.AsRedirect("~/account/#identityProviders");
                }

                return HttpStatusCode.BadRequest;
            };

            Post["/newpassword"] = _ =>
            {
                if (Context.CurrentUser == null)
                {
                    return HttpStatusCode.Forbidden;
                }

                string password = Request.Form.password;
                string confirmPassword = Request.Form.confirmPassword;

                ValidatePassword(password, confirmPassword);

                ChatUser user = repository.GetUserById(Context.CurrentUser.UserName);

                try
                {
                    if (ModelValidationResult.IsValid)
                    {
                        membershipService.SetUserPassword(user, password);
                        repository.CommitChanges();
                    }
                }
                catch (Exception ex)
                {
                    this.AddValidationError("_FORM", ex.Message);
                }

                if (ModelValidationResult.IsValid)
                {
                    this.AddAlertMessage("success", "Successfully added a password.");
                    return Response.AsRedirect("~/account/#changePassword");
                }

                return GetProfileView(authService, user);
            };

            Post["/changepassword"] = _ =>
            {
                if (Context.CurrentUser == null)
                {
                    return HttpStatusCode.Forbidden;
                }

                string oldPassword = Request.Form.oldPassword;
                string password = Request.Form.password;
                string confirmPassword = Request.Form.confirmPassword;

                if (String.IsNullOrEmpty(oldPassword))
                {
                    this.AddValidationError("oldPassword", "Old password is required");
                }

                ValidatePassword(password, confirmPassword);

                ChatUser user = repository.GetUserById(Context.CurrentUser.UserName);

                try
                {
                    if (ModelValidationResult.IsValid)
                    {
                        membershipService.ChangeUserPassword(user, oldPassword, password);
                        repository.CommitChanges();
                    }
                }
                catch (Exception ex)
                {
                    this.AddValidationError("_FORM", ex.Message);
                }

                if (ModelValidationResult.IsValid)
                {
                    this.AddAlertMessage("success", "Successfully changed your password.");
                    return Response.AsRedirect("~/account/#changePassword");
                }

                return GetProfileView(authService, user);
            };

            Post["/changeusername"] = _ =>
            {
                if (Context.CurrentUser == null)
                {
                    return HttpStatusCode.Forbidden;
                }

                string username = Request.Form.username;
                string confirmUsername = Request.Form.confirmUsername;

                ValidateUsername(username, confirmUsername);

                ChatUser user = repository.GetUserById(Context.CurrentUser.UserName);
                string oldUsername = user.Name;

                try
                {
                    if (ModelValidationResult.IsValid)
                    {
                        membershipService.ChangeUserName(user, username);
                        repository.CommitChanges();
                    }
                }
                catch (Exception ex)
                {
                    this.AddValidationError("_FORM", ex.Message);
                }

                if (ModelValidationResult.IsValid)
                {
                    notificationService.OnUserNameChanged(user, oldUsername, username);

                    this.AddAlertMessage("success", "Successfully changed your username.");
                    return Response.AsRedirect("~/account/#changeUsername");
                }

                return GetProfileView(authService, user);
            };
        }

        private void ValidatePassword(string password, string confirmPassword)
        {
            if (String.IsNullOrEmpty(password))
            {
                this.AddValidationError("password", "Password is required");
            }

            if (!String.Equals(password, confirmPassword))
            {
                this.AddValidationError("confirmPassword", "Passwords don't match");
            }
        }

        private void ValidateUsername(string username, string confirmUsername)
        {
            if (String.IsNullOrEmpty(username))
            {
                this.AddValidationError("username", "Username is required");
            }

            if (!String.Equals(username, confirmUsername))
            {
                this.AddValidationError("confirmUsername", "Usernames don't match");
            }
        }

        private dynamic GetProfileView(IAuthenticationService authService, ChatUser user)
        {
            return View["index", new ProfilePageViewModel(user, authService.GetProviders())];
        }

        private LoginViewModel GetLoginViewModel(IApplicationSettings applicationSettings,
                                                 IJabbrRepository repository,
                                                 IAuthenticationService authService)
        {
            ChatUser user = null;

            if (Context.CurrentUser != null)
            {
                user = repository.GetUserById(Context.CurrentUser.UserName);
            }

            var viewModel = new LoginViewModel(authService.GetProviders(), user != null ? user.Identities : null);
            return viewModel;
        }
    }
}