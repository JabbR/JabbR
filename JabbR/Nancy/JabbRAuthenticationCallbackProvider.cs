using System;
using System.Collections.Generic;
using System.Security.Claims;
using JabbR.Services;
using Nancy;
using Nancy.Authentication.WorldDomination;
using WorldDomination.Web.Authentication;

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
                nancyModule.Request.AddAlertMessage("error", model.Exception.Message);
            }
            else
            {
                UserInformation information = model.AuthenticatedClient.UserInformation;
                var claims = new List<Claim>();
                claims.Add(new Claim(ClaimTypes.NameIdentifier, information.Id));
                claims.Add(new Claim(ClaimTypes.AuthenticationMethod, model.AuthenticatedClient.ProviderName));

                if (!String.IsNullOrEmpty(information.UserName))
                {
                    claims.Add(new Claim(ClaimTypes.Name, information.UserName));
                }

                if (!String.IsNullOrEmpty(information.Email))
                {
                    claims.Add(new Claim(ClaimTypes.Email, information.Email));
                }


                nancyModule.SignIn(claims);
            }

            return response;
        }

        public dynamic OnRedirectToAuthenticationProviderError(NancyModule nancyModule, string errorMessage)
        {
            return null;
        }
    }
}
