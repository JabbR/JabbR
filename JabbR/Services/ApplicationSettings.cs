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
    }
}