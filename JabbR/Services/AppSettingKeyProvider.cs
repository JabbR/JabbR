using System;
using JabbR.Infrastructure;

namespace JabbR.Services
{
    public class AppSettingKeyProvider : IKeyProvider
    {
        public AppSettingKeyProvider(IApplicationSettings settings)
        {
            if (String.IsNullOrEmpty(settings.EncryptionKey))
            {
                throw new ArgumentException("Missing encryptionKey");
            }

            if (String.IsNullOrEmpty(settings.ValidationKey))
            {
                throw new ArgumentException("Missing validationKey");
            }

            EncryptionKey = CryptoHelper.FromHex(settings.EncryptionKey);
            ValidationKey = CryptoHelper.FromHex(settings.ValidationKey);
        }

        public byte[] EncryptionKey
        {
            get;
            private set;
        }

        public byte[] ValidationKey
        {
            get;
            private set;
        }
    }
}