using System;
using System.Configuration;

namespace JabbR.Services
{
    public class JabbrConfiguration : IJabbrConfiguration
    {
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

        public string DeploymentSha
        {
            get
            {
                return ConfigurationManager.AppSettings["jabbr:releaseSha"];
            }
        }

        public string DeploymentBranch
        {
            get
            {
                return ConfigurationManager.AppSettings["jabbr:releaseBranch"];
            }
        }

        public string DeploymentTime
        {
            get
            {
                return ConfigurationManager.AppSettings["jabbr:releaseTime"];
            }
        }
    }
}