using System.Text;
using System.Web;
using System.Web.Security;
using JabbR.Models;

namespace JabbR.Services
{
    public class AuthenticationTokenService : IAuthenticationTokenService
    {
        private readonly IJabbrRepository _repository;
        private static readonly UTF8Encoding _encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);
        private static readonly string UserIdPurpose = "JabbR.UserId";

        public AuthenticationTokenService(IJabbrRepository repository)
        {
            _repository = repository;
        }

        public bool TryGetUserId(string authenticationToken, out string userId)
        {
            try
            {
                byte[] buffer = HttpServerUtility.UrlTokenDecode(authenticationToken);

                buffer = MachineKey.Unprotect(buffer, UserIdPurpose);

                userId = _encoding.GetString(buffer);

                if (_repository.GetUserById(userId) != null)
                {
                    return true;
                }
            }
            catch
            {
                userId = null;
                return false;
            }

            return false;
        }

        public string GetAuthenticationToken(ChatUser user)
        {
            byte[] buffer = _encoding.GetBytes(user.Id);

            buffer = MachineKey.Protect(buffer, UserIdPurpose);

            return HttpServerUtility.UrlTokenEncode(buffer);
        }

        public void Dispose()
        {
            _repository.Dispose();
        }
    }
}