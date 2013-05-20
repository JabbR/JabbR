using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using JabbR.Models;
using JabbR.Services;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Forms;
using Newtonsoft.Json;
using Owin.Types;
using Owin.Types.Extensions;
using Owin.Types.Helpers;

namespace JabbR.Infrastructure
{
    public class JabbRFormsAuthenticationProvider : IFormsAuthenticationProvider
    {
        private readonly IJabbrRepository _repository;
        private readonly IMembershipService _membershipService;

        public JabbRFormsAuthenticationProvider(IJabbrRepository repository, IMembershipService membershipService)
        {
            _repository = repository;
            _membershipService = membershipService;
        }

        public Task ValidateIdentity(FormsValidateIdentityContext context)
        {
            return TaskAsyncHelper.Empty;
        }

        public Task ValidateLogin(FormsValidateLoginContext context)
        {
            return TaskAsyncHelper.Empty;
        }

        public void ResponseSignIn(FormsResponseSignInContext context)
        {
            var authResult = new AuthenticationResult
            {
                Success = true
            };

            ChatUser loggedInUser = GetLoggedInUser(context.Environment);

            var principal = new ClaimsPrincipal(context.Identity);

            // Do nothing if it's authenticated
            if (principal.IsAuthenticated())
            {
                EnsurePersistentCookie(context);
                return;
            }

            ChatUser user = _repository.GetUser(principal);
            authResult.ProviderName = principal.GetIdentityProvider();

            // The user exists so add the claim
            if (user != null)
            {
                if (loggedInUser != null && user != loggedInUser)
                {
                    // Set an error message
                    authResult.Message = String.Format("This {0} account has already been linked to another user.", authResult.ProviderName);
                    authResult.Success = false;

                    // Keep the old user logged in
                    context.Identity.AddClaim(new Claim(JabbRClaimTypes.Identifier, loggedInUser.Id));
                }
                else
                {
                    // Login this user
                    AddClaim(context, user);
                }

            }
            else if (principal.HasRequiredClaims())
            {
                ChatUser targetUser = null;

                // The user doesn't exist but the claims to create the user do exist
                if (loggedInUser == null)
                {
                    // New user so add them
                    user = _membershipService.AddUser(principal);

                    targetUser = user;
                }
                else
                {
                    // If the user is logged in then link
                    _membershipService.LinkIdentity(loggedInUser, principal);

                    _repository.CommitChanges();

                    authResult.Message = String.Format("Successfully linked {0} account.", authResult.ProviderName);

                    targetUser = loggedInUser;
                }

                AddClaim(context, targetUser);
            }
            else if(!principal.HasPartialIdentity())
            {
                // A partial identity means the user needs to add more claims to login
                context.Identity.AddClaim(new Claim(JabbRClaimTypes.PartialIdentity, "true"));
            }

            var response = new OwinResponse(context.Environment);
            var cookieOptions = new CookieOptions
            {
                HttpOnly = true
            };

            response.AddCookie(Constants.AuthResultCookie,
                               JsonConvert.SerializeObject(authResult),
                               cookieOptions);
        }

        private static void AddClaim(FormsResponseSignInContext context, ChatUser user)
        {
            // Add the jabbr id claim
            context.Identity.AddClaim(new Claim(JabbRClaimTypes.Identifier, user.Id));

            // Add the admin claim if the user is an Administrator
            if (user.IsAdmin)
            {
                context.Identity.AddClaim(new Claim(JabbRClaimTypes.Admin, "true"));
            }

            EnsurePersistentCookie(context);
        }

        private static void EnsurePersistentCookie(FormsResponseSignInContext context)
        {
            if (context.Extra == null)
            {
                context.Extra = new AuthenticationExtra();
            }

            context.Extra.IsPersistent = true;
        }

        private ChatUser GetLoggedInUser(IDictionary<string, object> env)
        {
            var request = new OwinRequest(env);

            var principal = request.User as ClaimsPrincipal;

            if (principal != null)
            {
                return _repository.GetLoggedInUser(principal);
            }

            return null;
        }
    }
}