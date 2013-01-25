using System;
using System.Security.Cryptography;
using System.Text;
using JabbR.Models;

namespace JabbR.Services
{
    public class AuthenticationTokenService : IAuthenticationTokenService
    {
        private readonly IJabbrRepository _repository;
        private static readonly UTF8Encoding _encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);
        private static readonly byte[] UserIdPurpose = _encoding.GetBytes("JabbR.UserId");

        public AuthenticationTokenService(IJabbrRepository repository)
        {
            _repository = repository;
        }

        public bool TryGetUserId(string authenticationToken, out string userId)
        {
            try
            {
                byte[] buffer = Convert.FromBase64String(authenticationToken);

                buffer = ProtectedData.Unprotect(buffer, UserIdPurpose, DataProtectionScope.CurrentUser);

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

            buffer = ProtectedData.Protect(buffer, UserIdPurpose, DataProtectionScope.CurrentUser);

            return Convert.ToBase64String(buffer);
        }

        public void Dispose()
        {
            _repository.Dispose();
        }
    }
}