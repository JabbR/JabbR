using System;
using System.Security.Cryptography;
using JabbR.Infrastructure;

namespace JabbR.Services
{
    public class CryptoService : ICryptoService
    {
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
    }
}