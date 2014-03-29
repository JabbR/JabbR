using System.Configuration;

namespace JabbR.Services
{
    public interface IJabbrConfiguration
    {
        bool RequireHttps { get; }
        bool MigrateDatabase { get; }

        string DeploymentSha { get; }
        string DeploymentBranch { get; }
        string DeploymentTime { get; }

        string ServiceBusConnectionString { get; }
        string ServiceBusTopicPrefix { get; }

        ConnectionStringSettings SqlConnectionString { get; }
        bool ScaleOutSqlServer { get; }
    }
}