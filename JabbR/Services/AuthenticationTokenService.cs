using System;
using System.Security.Cryptography;
using System.Text;
using JabbR.Models;

namespace JabbR.Services
{
    public class AuthenticationTokenService : IAuthenticationTokenService
    {
        private static readonly UTF8Encoding _encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);
        private readonly ICryptoService _cryptoService;

        public AuthenticationTokenService(ICryptoService cryptoService)
        {
            _cryptoService = cryptoService;
        }

        public bool TryGetUserId(string authenticationToken, out string userId)
        {
            try
            {
                byte[] buffer = TokenDencode(authenticationToken);

                buffer = _cryptoService.Unprotect(buffer);

                userId = _encoding.GetString(buffer);

                // REVIEW: Should we verify the user id with the db on every request?
                // it would need to be cached.
                return true;
            }
            catch
            {
                userId = null;
                return false;
            }
        }

        public string GetAuthenticationToken(ChatUser user)
        {
            byte[] buffer = _encoding.GetBytes(user.Id);

            buffer = _cryptoService.Protect(buffer);

            return TokenEncode(buffer);
        }

        private static string TokenEncode(byte[] buffer)
        {
            return Convert.ToBase64String(buffer).Replace('+', '.')
                                                 .Replace('/', '-')
                                                 .Replace('=', '_');
        }

        private static byte[] TokenDencode(string authenticationToken)
        {
            return Convert.FromBase64String(authenticationToken.Replace('.', '+').Replace('-', '/').Replace('_', '='));
        }
    }
}