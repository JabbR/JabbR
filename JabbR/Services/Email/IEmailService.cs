namespace JabbR.Services
{
    public interface IEmailService
    {
        void SendRequestResetPassword(Models.ChatUser user, string siteBaseUrl);
    }
}