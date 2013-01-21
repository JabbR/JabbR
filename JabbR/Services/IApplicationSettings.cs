
namespace JabbR.Services
{
    public interface IApplicationSettings
    {
        string DefaultAdminUserName { get; }
        string DefaultAdminPassword { get; }
        AuthenticationMode AuthenticationMode { get; }
    }
}
