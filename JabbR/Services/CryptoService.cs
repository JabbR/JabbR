using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using JabbR.Infrastructure;

namespace JabbR.Services
{
    public class CryptoService : ICryptoService
    {
        private const int TokenBytesLength = 13;

        private readonly IKeyProvider _keyProvider;

        public CryptoService(IKeyProvider keyProvider)
        {
            _keyProvider = keyProvider;
        }

        public string CreateSalt()
        {
            var data = new byte[0x10];

            using (var crypto = new RNGCryptoServiceProvider())
            {
                crypto.GetBytes(data);

                return Convert.ToBase64String(data);
            }
        }

        public byte[] Protect(byte[] plainText)
        {
            var initializationVector = new byte[16];
            using (var crypto = new RNGCryptoServiceProvider())
            {
                crypto.GetBytes(initializationVector);
                return CryptoHelper.Protect(_keyProvider.EncryptionKey, _keyProvider.VerificationKey, initializationVector, plainText);
            }
        }

        public byte[] Unprotect(byte[] payload)
        {
            return CryptoHelper.Unprotect(_keyProvider.EncryptionKey, _keyProvider.VerificationKey, payload);
        }

        public string CreateToken(string value)
        {
            var token = new byte[TokenBytesLength];
            var userNameBytes = Encoding.Default.GetBytes(value);

            using (var crypto = new RNGCryptoServiceProvider())
            {
                crypto.GetBytes(token);
                var tokenBytes = token.Concat(userNameBytes)
                                      .ToArray();

                return Convert.ToBase64String(tokenBytes);
            }
        }

        public string GetValueFromToken(string token)
        {
            var tokenBytes = Convert.FromBase64String(token);
            var valueBytes = tokenBytes.Skip(TokenBytesLength)
                                       .ToArray();

            return Encoding.Default.GetString(valueBytes);
        }
    }
}