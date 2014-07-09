using System;
using System.Collections.Generic;
using System.IO;
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
            DisabledContentProviders = new HashSet<string>();
        }

        public string EncryptionKey { get; set; }

        public string VerificationKey { get; set; }

        public string AzureblobStorageConnectionString { get; set; }

        public string LocalFileSystemStoragePath { get; set; }

        public string LocalFileSystemStorageUriPrefix { get; set; }

        public int MaxFileUploadBytes { get; set; }

        public int MaxMessageLength { get; set; }

        public string GoogleAnalytics { get; set; }

        public string AppInsights { get; set; }

        public bool AllowUserRegistration { get; set; }

        public bool AllowUserResetPassword { get; set; }

        public int RequestResetPasswordValidThroughInHours { get; set; }

        public bool AllowRoomCreation { get; set; }

        public string FacebookAppId { get; set; }

        public string FacebookAppSecret { get; set; }

        public string TwitterConsumerKey { get; set; }

        public string TwitterConsumerSecret { get; set; }

        public string GoogleClientID { get; set; }

        public string GoogleClientSecret { get; set; }

        public string EmailSender { get; set; }

        public List<ContentProviderSetting> ContentProviders { get; set; }

        public HashSet<string> DisabledContentProviders { get; set; }

        public static bool TryValidateSettings(ApplicationSettings settings, out IDictionary<string, string> errors)
        {
            errors = new Dictionary<string, string>();

            if (!String.IsNullOrEmpty(settings.LocalFileSystemStoragePath))
            {
                if (!Path.IsPathRooted(settings.LocalFileSystemStoragePath))
                {
                    errors.Add("LocalFileSystemStoragePath", "The path must be an absolute path");
                }
                else if (settings.LocalFileSystemStoragePath.StartsWith(AppDomain.CurrentDomain.SetupInformation.ApplicationBase, StringComparison.OrdinalIgnoreCase))
                {
                    errors.Add("LocalFileSystemStoragePath", "The path must not be under the JabbR root.");
                }
            }

            // TODO: Add more validation

            return errors.Count == 0;
        }

        public static ApplicationSettings GetDefaultSettings()
        {
            return new ApplicationSettings
            {
                EncryptionKey = CryptoHelper.ToHex(GenerateRandomBytes()),
                VerificationKey = CryptoHelper.ToHex(GenerateRandomBytes()),
                MaxFileUploadBytes = 5242880,
                MaxMessageLength = 0,
                AllowUserRegistration = true,
                AllowRoomCreation = true,
                AllowUserResetPassword = false,
                RequestResetPasswordValidThroughInHours = 6,
                EmailSender = String.Empty,
                ContentProviders = ContentProviderSetting.GetDefaultContentProviders()
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