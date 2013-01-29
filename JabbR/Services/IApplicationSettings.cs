
namespace JabbR.Services
{
    public interface IApplicationSettings
    {
        string EncryptionKey { get; }
        string ValidationKey { get; }

        string DefaultAdminUserName { get; }
        string DefaultAdminPassword { get; }
        AuthenticationMode AuthenticationMode { get; }
        bool RequireHttps { get; }
    }
}
