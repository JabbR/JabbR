using JabbR.Models;

namespace JabbR.Services
{
    public class AuthenticationTokenService : IAuthenticationTokenService
    {
        private readonly IJabbrRepository _repository;

        public AuthenticationTokenService(IJabbrRepository repository)
        {
            _repository = repository;
        }

        public bool TryGetUserId(string authenticationToken, out string userId)
        {
            try
            {
                userId = authenticationToken;

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

        public bool IsValidAuthenticationToken(string authenticationToken)
        {
            try
            {
                string userId = authenticationToken;
                return _repository.GetUserById(userId) != null;
            }
            catch
            {
                return false;
            }
        }

        public string GetAuthenticationToken(ChatUser user)
        {
            // TODO: encrypt and sign
            return user.Id;
        }

        public void Dispose()
        {
            _repository.Dispose();
        }
    }
}