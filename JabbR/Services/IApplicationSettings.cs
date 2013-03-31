
namespace JabbR.Services
{
    public interface IApplicationSettings
    {
        string EncryptionKey { get; }
        string VerificationKey { get; }

        string DefaultAdminUserName { get; }
        string DefaultAdminPassword { get; }

        bool RequireHttps { get; }
        bool MigrateDatabase { get; }
        bool ProxyImages { get; }
        int ProxyImageMaxSizeBytes { get; }

        string AzureblobStorageConnectionString { get; }

        int MaxFileUploadBytes { get; }
    }
}
