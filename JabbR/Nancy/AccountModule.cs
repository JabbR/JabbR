using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using JabbR.Infrastructure;
using JabbR.Models;
using JabbR.Services;
using JabbR.ViewModels;
using Nancy;
using WorldDomination.Web.Authentication;

namespace JabbR.Nancy
{
    public class AccountModule : JabbRModule
    {
        public AccountModule(ApplicationSettings applicationSettings,
                             IMembershipService membershipService,
                             IJabbrRepository repository,
                             IAuthenticationService authService,
                             IChatNotificationService notificationService,
                             IUserAuthenticator authenticator)
            : base("/account")
        {
            Get["/"] = _ =>
            {
                if (!IsAuthenticated)
                {
                    return HttpStatusCode.Forbidden;
                }

                ChatUser user = repository.GetUserById(Principal.GetUserId());

                return GetProfileView(authService, user);
            };

            Get["/login"] = _ =>
            {
                if (IsAuthenticated)
                {
                    return Response.AsRedirect("~/");
                }

                return View["login", GetLoginViewModel(applicationSettings, repository, authService)];
            };

            Post["/login"] = param =>
            {
                if (IsAuthenticated)
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
                        IList<Claim> claims;
                        if (authenticator.TryAuthenticateUser(username, password, out claims))
                        {
                            return this.SignIn(claims);
                        }
                    }
                }
                catch
                {
                    // Swallow the exception    
                }

                this.AddValidationError("_FORM", "Login failed. Check your username/password.");

                return View["login", GetLoginViewModel(applicationSettings, repository, authService)];
            };

            Post["/logout"] = _ =>
            {
                if (!IsAuthenticated)
                {
                    return HttpStatusCode.Forbidden;
                }

                var response = Response.AsJson(new { success = true });

                this.SignOut();

                return response;
            };

            Get["/register"] = _ =>
            {
                if (IsAuthenticated)
                {
                    return Response.AsRedirect("~/");
                }

                bool requirePassword = !Principal.Identity.IsAuthenticated;

                if (requirePassword && 
                    !applicationSettings.AllowUserRegistration)
                {
                    return HttpStatusCode.NotFound;
                }

                ViewBag.requirePassword = requirePassword;

                return View["register"];
            };

            Post["/create"] = _ =>
            {
                bool requirePassword = !Principal.Identity.IsAuthenticated;

                if (requirePassword &&
                    !applicationSettings.AllowUserRegistration)
                {
                    return HttpStatusCode.NotFound;
                }

                if (IsAuthenticated)
                {
                    return Response.AsRedirect("~/");
                }

                ViewBag.requirePassword = requirePassword;

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

                try
                {
                    if (requirePassword)
                    {
                        ValidatePassword(password, confirmPassword);
                    }

                    if (ModelValidationResult.IsValid)
                    {
                        if (requirePassword)
                        {
                            ChatUser user = membershipService.AddUser(username, email, password);

                            return this.SignIn(user);
                        }
                        else
                        {
                            // Add the required claims to this identity
                            var identity = Principal.Identity as ClaimsIdentity;

                            if (!Principal.HasClaim(ClaimTypes.Name))
                            {
                                identity.AddClaim(new Claim(ClaimTypes.Name, username));
                            }

                            if (!Principal.HasClaim(ClaimTypes.Email))
                            {
                                identity.AddClaim(new Claim(ClaimTypes.Email, email));
                            }

                            return this.SignIn(Principal.Claims);
                        }
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
                if (!IsAuthenticated)
                {
                    return HttpStatusCode.Forbidden;
                }

                string provider = Request.Form.provider;
                ChatUser user = repository.GetUserById(Principal.GetUserId());

                if (user.Identities.Count == 1 && !user.HasUserNameAndPasswordCredentials())
                {
                    Request.AddAlertMessage("error", "You cannot unlink this account because you would lose your ability to login.");
                    return Response.AsRedirect("~/account/#identityProviders");
                }

                var identity = user.Identities.FirstOrDefault(i => i.ProviderName == provider);

                if (identity != null)
                {
                    repository.Remove(identity);

                    Request.AddAlertMessage("success", String.Format("Successfully unlinked {0} account.", provider));
                    return Response.AsRedirect("~/account/#identityProviders");
                }

                return HttpStatusCode.BadRequest;
            };

            Post["/newpassword"] = _ =>
            {
                if (!IsAuthenticated)
                {
                    return HttpStatusCode.Forbidden;
                }

                string password = Request.Form.password;
                string confirmPassword = Request.Form.confirmPassword;

                ValidatePassword(password, confirmPassword);

                ChatUser user = repository.GetUserById(Principal.GetUserId());

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
                    Request.AddAlertMessage("success", "Successfully added a password.");
                    return Response.AsRedirect("~/account/#changePassword");
                }

                return GetProfileView(authService, user);
            };

            Post["/changepassword"] = _ =>
            {
                if (!applicationSettings.AllowUserRegistration)
                {
                    return HttpStatusCode.NotFound;
                }

                if (!IsAuthenticated)
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

                ChatUser user = repository.GetUserById(Principal.GetUserId());

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
                    Request.AddAlertMessage("success", "Successfully changed your password.");
                    return Response.AsRedirect("~/account/#changePassword");
                }

                return GetProfileView(authService, user);
            };

            Post["/changeusername"] = _ =>
            {
                if (!IsAuthenticated)
                {
                    return HttpStatusCode.Forbidden;
                }

                string username = Request.Form.username;
                string confirmUsername = Request.Form.confirmUsername;

                ValidateUsername(username, confirmUsername);

                ChatUser user = repository.GetUserById(Principal.GetUserId());
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

                    Request.AddAlertMessage("success", "Successfully changed your username.");
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

        private LoginViewModel GetLoginViewModel(ApplicationSettings applicationSettings,
                                                 IJabbrRepository repository,
                                                 IAuthenticationService authService)
        {
            ChatUser user = null;

            if (IsAuthenticated)
            {
                user = repository.GetUserById(Principal.GetUserId());
            }

            var viewModel = new LoginViewModel(applicationSettings, authService.GetProviders(), user != null ? user.Identities : null);
            return viewModel;
        }
    }
}