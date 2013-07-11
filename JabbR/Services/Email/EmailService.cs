using JabbR.Models;

namespace JabbR.Services
{
    public class EmailService : IEmailService
    {
        public const string RequestResetPasswordTemplateName = "RequestResetPassword";

        private readonly ApplicationSettings _applicationSettings;
        private readonly IEmailTemplateEngine _templateEngine;
        private readonly IEmailSender _sender;

        public EmailService(ApplicationSettings applicationSettings,
                            IEmailTemplateEngine templateEngine,
                            IEmailSender sender)
        {
            _applicationSettings = applicationSettings;
            _templateEngine = templateEngine;
            _sender = sender;
        }

        public void SendRequestResetPassword(ChatUser user, string siteBaseUrl)
        {
            var model = new
            {
                From = _applicationSettings.EmailSender,
                To = user.Email,
                Name = user.Name,
                PasswordResetUrl = siteBaseUrl + user.RequestPasswordResetId
            };

            var mail = _templateEngine.RenderTemplate(RequestResetPasswordTemplateName, model);
            _sender.Send(mail);
        }
    }
}