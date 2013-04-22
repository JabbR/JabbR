using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using JabbR.Infrastructure;
using JabbR.Models;
using Newtonsoft.Json;
using Ninject;
using Ninject.Activation;

namespace JabbR.Services
{
    public class ApplicationSettings
    {
        private static readonly TimeSpan _settingsCacheTimespan = TimeSpan.FromDays(1);
        private static readonly string _jabbrSettingsCacheKey = "jabbr.settings";

        public string EncryptionKey { get; set; }

        public string VerificationKey { get; set; }

        public string AzureblobStorageConnectionString { get; set; }

        public int MaxFileUploadBytes { get; set; }

        public string GoogleAnalytics { get; set; }

        public IDictionary<string, string> AuthenticationProviders { get; set; }

        public static ApplicationSettings Load(IContext context)
        {
            var cache = context.Kernel.Get<ICache>();
            var settings = (ApplicationSettings)cache.Get(_jabbrSettingsCacheKey);

            if (settings == null)
            {
                using (var dbContext = context.Kernel.Get<JabbrContext>())
                {
                    Settings dbSettings = dbContext.Settings.FirstOrDefault();

                    if (dbSettings == null)
                    {
                        // Create the initial app settings
                        settings = GetDefaultSettings();
                        dbSettings = new Settings
                        {
                            RawSettings = JsonConvert.SerializeObject(settings)
                        };

                        dbContext.Settings.Add(dbSettings);
                        dbContext.SaveChanges();
                    }
                    else
                    {
                        settings = JsonConvert.DeserializeObject<ApplicationSettings>(dbSettings.RawSettings);
                    }
                }

                // Cache the settings forever (until it changes)
                cache.Set(_jabbrSettingsCacheKey, settings, _settingsCacheTimespan);
            }

            return settings;
        }

        private static ApplicationSettings GetDefaultSettings()
        {
            return new ApplicationSettings
            {
                EncryptionKey = CryptoHelper.ToHex(GenerateRandomBytes()),
                VerificationKey = CryptoHelper.ToHex(GenerateRandomBytes()),
                MaxFileUploadBytes = 5242880,
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