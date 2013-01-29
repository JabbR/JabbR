using System;
using System.Security.Cryptography;
using System.Text;
using JabbR.Infrastructure;
using Xunit;

namespace JabbR.Tests
{
    public class CryptoHelperFacts
    {
        [Fact]
        public void ProtectCanUnProtect()
        {
            byte[] encryptionKey = GenerateRandomBytes();
            byte[] validationKey = GenerateRandomBytes();

            using (var algo = new AesCryptoServiceProvider())
            {
                byte[] bytes = Encoding.UTF8.GetBytes("Hello World");
                byte[] payload = CryptoHelper.Protect(encryptionKey, validationKey, algo.IV, bytes);

                byte[] buffer = CryptoHelper.Unprotect(encryptionKey, validationKey, payload);

                Assert.Equal("Hello World", Encoding.UTF8.GetString(buffer));
            }
        }

        [Fact]
        public void WrongValidationKeyWontDecrypt()
        {
            byte[] encryptionKey = GenerateRandomBytes();
            byte[] validationKey = GenerateRandomBytes();

            using (var algo = new AesCryptoServiceProvider())
            {
                byte[] bytes = Encoding.UTF8.GetBytes("Hello World");
                byte[] payload = CryptoHelper.Protect(encryptionKey, validationKey, algo.IV, bytes);
                Assert.Throws<InvalidOperationException>(() => CryptoHelper.Unprotect(encryptionKey, GenerateRandomBytes(), payload));
            }
        }

        [Fact]
        public void WrongPayloadDecrypt()
        {
            byte[] encryptionKey = GenerateRandomBytes();
            byte[] validationKey = GenerateRandomBytes();

            using (var algo = new AesCryptoServiceProvider())
            {
                byte[] bytes = Encoding.UTF8.GetBytes("Hello World");
                CryptoHelper.Protect(encryptionKey, validationKey, algo.IV, bytes);
                Assert.Throws<InvalidOperationException>(() => CryptoHelper.Unprotect(encryptionKey, validationKey, GenerateRandomBytes()));
            }
        }

        private byte[] GenerateRandomBytes(int n = 32)
        {
            using (var cryptoProvider = new RNGCryptoServiceProvider())
            {
                var bytes = new byte[n];
                cryptoProvider.GetBytes(bytes);
                return bytes;
            }
        }
    }
}
