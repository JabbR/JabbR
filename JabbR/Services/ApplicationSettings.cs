using System.Collections.Generic;
using System.Security.Cryptography;
using JabbR.Infrastructure;

namespace JabbR.Services
{
    public class ApplicationSettings
    {
        public ApplicationSettings()
        {
            AllowUserRegistration = true;
            AllowRoomCreation = true;
        }

        public string EncryptionKey { get; set; }

        public string VerificationKey { get; set; }

        public string AzureblobStorageConnectionString { get; set; }

        public int MaxFileUploadBytes { get; set; }

        public string GoogleAnalytics { get; set; }

        public bool AllowUserRegistration { get; set; }

        public bool AllowRoomCreation { get; set; }

        public IDictionary<string, string> AuthenticationProviders { get; set; }

        public static ApplicationSettings GetDefaultSettings()
        {
            return new ApplicationSettings
            {
                EncryptionKey = CryptoHelper.ToHex(GenerateRandomBytes()),
                VerificationKey = CryptoHelper.ToHex(GenerateRandomBytes()),
                MaxFileUploadBytes = 5242880,
                AllowUserRegistration = true,
                AllowRoomCreation = true
            };
        }

        private static byte[] GenerateRandomBytes(int n = 32)
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