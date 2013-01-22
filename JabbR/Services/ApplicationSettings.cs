using System;
using System.Configuration;

namespace JabbR.Services
{
    public class ApplicationSettings : IApplicationSettings
    {
        public string DefaultAdminUserName
        {
            get
            {
                return ConfigurationManager.AppSettings["defaultAdminUserName"];
            }
        }

        public string DefaultAdminPassword
        {
            get
            {
                return ConfigurationManager.AppSettings["defaultAdminPassword"];
            }
        }

        public AuthenticationMode AuthenticationMode
        {
            get
            {
                return GetAuthenticationMode();
            }
        }

        public bool RequireHttps
        {
            get
            {
                string requireHttpsValue = ConfigurationManager.AppSettings["requireHttps"];
                bool requireHttps;
                if (Boolean.TryParse(requireHttpsValue, out requireHttps))
                {
                    return requireHttps;
                }
                return false;
            }
        }

        public static AuthenticationMode GetAuthenticationMode()
        {
            string modeValue = ConfigurationManager.AppSettings["authenticationMode"];
            AuthenticationMode mode;
            if (Enum.TryParse<AuthenticationMode>(modeValue, out mode))
            {
                return mode;
            }

            return AuthenticationMode.UsernamePassword;
        }
    }
}