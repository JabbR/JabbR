using System;
using JabbR.Infrastructure;

namespace JabbR.Services
{
    public class SettingsKeyProvider : IKeyProvider
    {
        public SettingsKeyProvider(ApplicationSettings settings)
        {
            if (String.IsNullOrEmpty(settings.EncryptionKey))
            {
                throw new ArgumentException("Missing encryptionKey");
            }

            if (String.IsNullOrEmpty(settings.VerificationKey))
            {
                throw new ArgumentException("Missing validationKey");
            }

            EncryptionKey = CryptoHelper.FromHex(settings.EncryptionKey);
            VerificationKey = CryptoHelper.FromHex(settings.VerificationKey);
        }

        public byte[] EncryptionKey
        {
            get;
            private set;
        }

        public byte[] VerificationKey
        {
            get;
            private set;
        }
    }
}