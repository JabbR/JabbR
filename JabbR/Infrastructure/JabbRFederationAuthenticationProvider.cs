using System.Threading.Tasks;
using JabbR.Services;
using Microsoft.Owin.Security.Federation;

namespace JabbR.Infrastructure
{
    public class JabbRFederationAuthenticationProvider : FederationAuthenticationProvider
    {
        private readonly IMembershipService _membershipService;
        public JabbRFederationAuthenticationProvider(IMembershipService membershipService)
        {
            _membershipService = membershipService;
        }

        public override Task SecurityTokenValidated(SecurityTokenValidatedContext context)
        {
            // Create the user if necessary
            _membershipService.GetOrAddUser(context.ClaimsPrincipal);
            
            return base.SecurityTokenValidated(context);
        }
    }
}