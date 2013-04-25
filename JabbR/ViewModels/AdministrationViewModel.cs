using JabbR.Services;
using System.Collections.Generic;

namespace JabbR.ViewModels
{
    public class AdministrationViewModel
    {
        public AdministrationViewModel(ApplicationSettings applicationSettings)
        {
            EncryptionKey = applicationSettings.EncryptionKey;
            VerificationKey = applicationSettings.VerificationKey;

            AzureblobStorageConnectionString = applicationSettings.AzureblobStorageConnectionString;
            MaxFileUploadBytes = applicationSettings.MaxFileUploadBytes;

            GoogleAnalytics = applicationSettings.GoogleAnalytics;

            AuthenticationProviders = applicationSettings.AuthenticationProviders;
        }

        public string EncryptionKey { get; set; }
        public string VerificationKey { get; set; }

        public string AzureblobStorageConnectionString { get; set; }

        public int MaxFileUploadBytes { get; set; }
        
        public string GoogleAnalytics { get; set; }
        
        public IDictionary<string, string> AuthenticationProviders { get; set; }

    }
}