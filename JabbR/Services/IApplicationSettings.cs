
namespace JabbR.Services
{
    public interface IApplicationSettings
    {
        string EncryptionKey { get; }
        string VerificationKey { get; }

        string DefaultAdminUserName { get; }
        string DefaultAdminPassword { get; }
        AuthenticationMode AuthenticationMode { get; }

        bool RequireHttps { get; }
        bool MigrateDatabase { get; }
    }
}
