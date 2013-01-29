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

        [Fact]
        public void ToHex()
        {
            var buffer = new byte[] { 1, 2, 3, 4, 25, 15 };

            var bitConv = BitConverter.ToString(buffer).Replace("-", "");
            var hex = CryptoHelper.ToHex(buffer);
            Assert.Equal(bitConv, hex);
            Assert.Equal("01020304190F", hex);
        }

        [Fact]
        public void FromHex()
        {
            string value = "01020304190F";
            var buffer = new byte[] { 1, 2, 3, 4, 25, 15 };

            byte[] resut = CryptoHelper.FromHex(value);

            Assert.Equal(buffer, resut);
        }

        [Fact]
        public void ToAndFromHex()
        {
            for (int i = 0; i < 20; i++)
            {
                var buffer = GenerateRandomBytes();
                string hex = CryptoHelper.ToHex(buffer);
                var bitConverter = BitConverter.ToString(buffer).Replace("-", "");                
                Assert.Equal(bitConverter, hex);
                
                var fromBytes = CryptoHelper.FromHex(hex);
                Assert.Equal(buffer, fromBytes);
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
