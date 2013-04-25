using JabbR.Infrastructure;
using JabbR.Services;
using JabbR.ViewModels;
using Nancy;

namespace JabbR.Nancy
{
    public class AdministrationModule : JabbRModule
    {
        public AdministrationModule(ApplicationSettings applicationSettings,
                                    ISettingsManager settingsManager,
                                    IJabbrRepository repository)
            : base("/administration")
        {
            Get["/"] = _ =>
            {
                var user = repository.GetUserById(Principal.GetUserId());
                if (!IsAuthenticated || !user.IsAdmin)
                {
                    return HttpStatusCode.Forbidden;
                }
                return View["index", applicationSettings];
            };

            Post["/"] = _ =>
            {
                if (!IsAuthenticated)
                {
                    return HttpStatusCode.Forbidden;
                }

                applicationSettings.AzureblobStorageConnectionString = Request.Form.azureBlobStorageConnectionString;
                applicationSettings.MaxFileUploadBytes = Request.Form.maxFileUploadBytes;

                applicationSettings.GoogleAnalytics = Request.Form.googleAnalytics;

                settingsManager.Save(applicationSettings);

                return View["index", applicationSettings];
            };
        }
    }
}