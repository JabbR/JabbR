using JabbR.Models;
using JabbR.Nancy;
using Nancy;
using Nancy.Authentication.WorldDomination;
using WorldDomination.Web.Authentication;

namespace JabbR.Services
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
                string providerName = model.AuthenticatedClient.ProviderType.ToString();

                ChatUser user = _repository.GetUserByIdentity(providerName, userInfo.Id) ??
                                _membershipService.AddUser(userInfo.UserName, providerName, userInfo.Id, userInfo.Email);

                return nancyModule.CompleteLogin(_authenticationTokenService, user);
            }

            // TODO: Handle errors better
            return nancyModule.Response.AsRedirect("~/");
        }
    }
}
