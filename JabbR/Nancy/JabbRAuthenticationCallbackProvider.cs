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
            if (model.Exception == null)
            {
                UserInformation userInfo = model.AuthenticatedClient.UserInformation;
                string providerName = model.AuthenticatedClient.ProviderType;

                ChatUser user = _repository.GetUserByIdentity(providerName, userInfo.Id);

                // User with that identity doesn't exist, check if a user is logged in
                if (user == null)
                {
                    if (nancyModule.Context.CurrentUser != null)
                    {
                        // Link to the logged in user
                        user = _repository.GetUserById(nancyModule.Context.CurrentUser.UserName);

                        LinkIdentity(userInfo, providerName, user);
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

                return nancyModule.CompleteLogin(_authenticationTokenService, user);
            }

            // TODO: Handle errors better
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
