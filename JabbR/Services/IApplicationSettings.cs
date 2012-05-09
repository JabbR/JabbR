
namespace JabbR.Services
{
    public interface IApplicationSettings
    {
        string AuthApiKey { get; }

        string DefaultAdminUserName { get; }

        string DefaultAdminPassword { get; }
    }
}
