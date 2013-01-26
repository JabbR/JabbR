using JabbR.Models;
using JabbR.Nancy;
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

                if (user == null)
                {
                    // User with that identity doesn't exist, check if a user is logged in
                    if (nancyModule.Context.CurrentUser != null)
                    {
                        user = _repository.GetUserById(nancyModule.Context.CurrentUser.UserName);

                        // Link this new identity
                        user.Identities.Add(new ChatUserIdentity
                        {
                            Email = userInfo.Email,
                            Identity = userInfo.Id,
                            ProvierName = providerName
                        });

                        _repository.CommitChanges();
                    }
                    else
                    {
                        // User doesn't exist
                        user = _membershipService.AddUser(userInfo.UserName, providerName, userInfo.Id, userInfo.Email);
                    }
                }

                return nancyModule.CompleteLogin(_authenticationTokenService, user);
            }

            // TODO: Handle errors better
            return nancyModule.Response.AsRedirect("~/");
        }
    }
}
