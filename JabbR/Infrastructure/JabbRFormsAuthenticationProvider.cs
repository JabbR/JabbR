using System.Threading.Tasks;
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
    }
}