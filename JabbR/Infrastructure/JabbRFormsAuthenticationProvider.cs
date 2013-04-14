using System.Security.Claims;
using System.Threading.Tasks;
using JabbR.Models;
using JabbR.Services;
using Microsoft.Owin.Security.Forms;

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
            var identity = context.Identity as ClaimsIdentity;

            var principal = new ClaimsPrincipal(identity);

            // Do nothing if it's authenticated
            if (principal.IsAuthenticated())
            {
                return;
            }

            ChatUser user = _repository.GetUser(principal);

            if (user != null)
            {
                identity.AddClaim(new Claim(JabbRClaimTypes.Identifier, user.Id));
            }
            else if (principal.HasRequiredClaims())
            {
                user = _membershipService.AddUser(principal);

                identity.AddClaim(new Claim(JabbRClaimTypes.Identifier, user.Id));
            }
        }
    }
}