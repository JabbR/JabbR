using System.Configuration;

namespace JabbR.Services
{
    public class ApplicationSettings : IApplicationSettings
    {
        public string AuthApiKey
        {
            get
            {
                return ConfigurationManager.AppSettings["auth.apiKey"];
            }
        }

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
    }
}