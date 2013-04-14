using System;
using System.Collections.Generic;
using System.Security.Claims;
using JabbR.Infrastructure;
using JabbR.Models;
using JabbR.Services;
using Nancy;
using Nancy.Authentication.WorldDomination;

namespace JabbR.Nancy
{
    public class JabbRAuthenticationCallbackProvider : IAuthenticationCallbackProvider
    {
        private readonly IJabbrRepository _repository;

        public JabbRAuthenticationCallbackProvider(IJabbrRepository repository)
        {
            _repository = repository;
        }

        public dynamic Process(NancyModule nancyModule, AuthenticateCallbackData model)
        {
            if (model.Exception != null)
            {
                nancyModule.AddAlertMessage("error", model.Exception.Message);

                return nancyModule.Response.AsRedirect("~/");
            }

            var claims = new List<Claim>();
            claims.Add(new Claim(ClaimTypes.NameIdentifier, model.AuthenticatedClient.UserInformation.Id));
            claims.Add(new Claim(ClaimTypes.Name, model.AuthenticatedClient.UserInformation.UserName));
            claims.Add(new Claim(ClaimTypes.Email, model.AuthenticatedClient.UserInformation.Email));
            claims.Add(new Claim(ClaimTypes.AuthenticationMethod, model.AuthenticatedClient.ProviderName));
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);

            string currentUserId = nancyModule.GetPrincipal().GetUserId();
            bool isAuthenticated = !String.IsNullOrEmpty(currentUserId);

            ChatUser user = _repository.GetUser(principal);
            Response response = nancyModule.Response.AsRedirect("~/");

            // If a user is logged in, then they got here from the account page, send them back there
            if (isAuthenticated)
            {
                response = nancyModule.Response.AsRedirect("~/account/#identityProviders");
            }

            if (isAuthenticated && user != null && user.Id != currentUserId)
            {
                // User already linked so fail here
                nancyModule.AddAlertMessage("error", String.Format("This {0} account has already been linked to another user.", model.AuthenticatedClient.ProviderName));

                return response;
            }
            else if (isAuthenticated)
            {
                // REVIEW: This is a little hacky since the operation might fail
                nancyModule.AddAlertMessage("success", String.Format("Successfully linked {0} account.", model.AuthenticatedClient.ProviderName));
            }

            nancyModule.SignIn(claims);

            return response;
        }
    }
}
