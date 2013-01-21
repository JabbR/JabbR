using JabbR.Models;

namespace JabbR.Services
{
    public class AuthenticationService : IAuthenticationService
    {
        private readonly IJabbrRepository _repository;

        public AuthenticationService(IJabbrRepository repository)
        {
            _repository = repository;
        }

        public bool TryGetUserId(string userToken, out string userId)
        {
            try
            {
                userId = userToken;

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

        public bool IsUserAuthenticated(string userToken)
        {
            try
            {
                string userId = userToken;
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