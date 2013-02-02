using System;
using System.Configuration;

namespace JabbR.Services
{
    public class ApplicationSettings : IApplicationSettings
    {
        public string EncryptionKey
        {
            get
            {
                return ConfigurationManager.AppSettings["jabbr:encryptionKey"];
            }
        }

        public string VerificationKey
        {
            get
            {
                return ConfigurationManager.AppSettings["jabbr:verificationKey"];
            }
        }

        public string DefaultAdminUserName
        {
            get
            {
                return ConfigurationManager.AppSettings["jabbr:defaultAdminUserName"];
            }
        }

        public string DefaultAdminPassword
        {
            get
            {
                return ConfigurationManager.AppSettings["jabbr:defaultAdminPassword"];
            }
        }

        public AuthenticationMode AuthenticationMode
        {
            get
            {
                string modeValue = ConfigurationManager.AppSettings["jabbr:authenticationMode"];
                AuthenticationMode mode;
                if (Enum.TryParse<AuthenticationMode>(modeValue, out mode))
                {
                    return mode;
                }

                return AuthenticationMode.Default;
            }
        }

        public bool RequireHttps
        {
            get
            {
                string requireHttpsValue = ConfigurationManager.AppSettings["jabbr:requireHttps"];
                bool requireHttps;
                if (Boolean.TryParse(requireHttpsValue, out requireHttps))
                {
                    return requireHttps;
                }
                return false;
            }
        }

        public bool MigrateDatabase
        {
            get
            {
                string migrateDatabaseValue = ConfigurationManager.AppSettings["jabbr:migrateDatabase"];
                bool migrateDatabase;
                if (Boolean.TryParse(migrateDatabaseValue, out migrateDatabase))
                {
                    return migrateDatabase;
                }
                return false;
            }
        }
    }
}