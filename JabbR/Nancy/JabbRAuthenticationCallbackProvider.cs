using System.Collections.Generic;
using System.Security.Claims;
using Nancy;
using Nancy.Authentication.WorldDomination;

namespace JabbR.Nancy
{
    public class JabbRAuthenticationCallbackProvider : IAuthenticationCallbackProvider
    {
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

            return nancyModule.SignIn(claims);
        }
    }
}
