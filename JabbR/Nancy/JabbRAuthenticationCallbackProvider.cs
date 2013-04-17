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
            Response response = nancyModule.Response.AsRedirect("~/");

            if (nancyModule.IsAuthenticated())
            {
                response = nancyModule.Response.AsRedirect("~/account/#identityProviders");
            }

            if (model.Exception != null)
            {
                nancyModule.AddAlertMessage("error", model.Exception.Message);
            }
            else
            {
                var claims = new List<Claim>();
                claims.Add(new Claim(ClaimTypes.NameIdentifier, model.AuthenticatedClient.UserInformation.Id));
                claims.Add(new Claim(ClaimTypes.Name, model.AuthenticatedClient.UserInformation.UserName));
                claims.Add(new Claim(ClaimTypes.Email, model.AuthenticatedClient.UserInformation.Email));
                claims.Add(new Claim(ClaimTypes.AuthenticationMethod, model.AuthenticatedClient.ProviderName));

                nancyModule.SignIn(claims);
            }

            return response;
        }
    }
}
