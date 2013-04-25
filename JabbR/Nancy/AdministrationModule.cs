using JabbR.Infrastructure;
using JabbR.Services;
using JabbR.ViewModels;
using Nancy;
using Nancy.ModelBinding;

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
                if (!Principal.HasClaim(JabbRClaimTypes.Admin))
                {
                    return HttpStatusCode.Forbidden;
                }

                
                return View["index", applicationSettings];
            };

            Post["/"] = _ =>
            {
                if (!Principal.HasClaim(JabbRClaimTypes.Admin))
                {
                    return HttpStatusCode.Forbidden;
                }

                var appSettings = this.BindTo(applicationSettings);

                settingsManager.Save(appSettings);

                return Response.AsRedirect("~/administration");
            };
        }
    }
}