using System;
using JabbR.Infrastructure;
using JabbR.Models;
using JabbR.Services;
using Nancy;
using Nancy.Authentication.WorldDomination;
using WorldDomination.Web.Authentication;

namespace JabbR.Nancy
{
    public class JabbRAuthenticationCallbackProvider : IAuthenticationCallbackProvider
    {
        private readonly IAuthenticationTokenService _authenticationTokenService;
        private readonly IMembershipService _membershipService;
        private readonly IJabbrRepository _repository;

        public JabbRAuthenticationCallbackProvider(IAuthenticationTokenService authenticationTokenService,
                                                   IMembershipService membershipService,
                                                   IJabbrRepository repository)
        {
            _authenticationTokenService = authenticationTokenService;
            _membershipService = membershipService;
            _repository = repository;
        }

        public dynamic Process(NancyModule nancyModule, AuthenticateCallbackData model)
        {
            ChatUser loggedInUser = null;

            if (nancyModule.Context.CurrentUser != null)
            {
                loggedInUser = _repository.GetUserById(nancyModule.Context.CurrentUser.UserName);
            }

            if (model.Exception == null)
            {
                UserInformation userInfo = model.AuthenticatedClient.UserInformation;
                string providerName = model.AuthenticatedClient.ProviderName;

                ChatUser user = _repository.GetUserByIdentity(providerName, userInfo.Id);

                // User with that identity doesn't exist, check if a user is logged in
                if (user == null)
                {
                    if (loggedInUser != null)
                    {
                        // Link to the logged in user
                        LinkIdentity(userInfo, providerName, loggedInUser);

                        // If a user is already logged in, then we know they could only have gotten here via the account page,
                        // so we will redirect them there
                        nancyModule.AddAlertMessage("success", String.Format("Successfully linked {0} account.", providerName));
                        return nancyModule.Response.AsRedirect("~/account/#identityProviders");
                    }
                    else
                    {
                        // Check the identity field to see if we need to migrate this user to the new
                        // non janrain identity model
                        string legacyIdentity = IdentityUtility.MakeLegacyIdentity(providerName, userInfo.Id);

                        if (legacyIdentity == null)
                        {
                            // No identity found so just add a new user
                            user = _membershipService.AddUser(userInfo.UserName, providerName, userInfo.Id, userInfo.Email);
                        }
                        else
                        {
                            // Try to get a legacy identity
                            user = _repository.GetUserByLegacyIdentity(legacyIdentity);

                            if (user == null)
                            {
                                // User doesn't exist
                                user = _membershipService.AddUser(userInfo.UserName, providerName, userInfo.Id, userInfo.Email);
                            }
                            else
                            {
                                // We found a legacy user via this id so convert them to the new format
                                LinkIdentity(userInfo, providerName, user);
                            }
                        }
                    }
                }
                else if (loggedInUser != null && user != loggedInUser)
                {
                    // You can't link an account that's already attached to another user
                    nancyModule.AddAlertMessage("error", String.Format("This {0} account has already been linked to another user.", providerName));
                    
                    // If a user is logged in then we know they got here from the account page, and we should redirect them back there
                    return nancyModule.Response.AsRedirect("~/account/#identityProviders");
                }

                return nancyModule.CompleteLogin(_authenticationTokenService, user);
            }

            nancyModule.AddAlertMessage("error", model.Exception.Message);

            // If a user is logged in, then they got here from the account page, send them back there
            if (loggedInUser != null)
            {
                return nancyModule.Response.AsRedirect("~/account/#identityProviders");
            }

            // At this point, send the user back to the root, everything else will work itself out
            return nancyModule.Response.AsRedirect("~/");
        }

        private void LinkIdentity(UserInformation userInfo, string providerName, ChatUser user)
        {
            // Link this new identity
            user.Identities.Add(new ChatUserIdentity
            {
                Email = userInfo.Email,
                Identity = userInfo.Id,
                ProviderName = providerName
            });

            _repository.CommitChanges();
        }
    }
}
