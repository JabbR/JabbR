using System;
using JabbR.Infrastructure;
using JabbR.Services;
using Nancy;
using Nancy.ModelBinding;

namespace JabbR.Nancy
{
    public class AdministrationModule : JabbRModule
    {
        public AdministrationModule(ApplicationSettings applicationSettings,
                                    ISettingsManager settingsManager)
            : base("/administration")
        {
            Get["/"] = _ =>
            {
                if (!IsAuthenticated || !Principal.HasClaim(JabbRClaimTypes.Admin))
                {
                    return HttpStatusCode.Forbidden;
                }

                return View["index", applicationSettings];
            };

            Post["/"] = _ =>
            {
                if (!IsAuthenticated || !Principal.HasClaim(JabbRClaimTypes.Admin))
                {
                    return HttpStatusCode.Forbidden;
                }

                ApplicationSettings appSettings = applicationSettings;

                try
                {
                    appSettings = this.Bind<ApplicationSettings>();
                    // The allow user registration field is not passed in the post
                    // when you uncheck it, so need to force the value to false
                    if (Request.Form.allowUserRegistration == null)
                    {
                        appSettings.AllowUserRegistration = false;
                    }
                    settingsManager.Save(appSettings);
                }
                catch (Exception ex)
                {
                    this.AddValidationError("_FORM", ex.Message);
                }

                if (ModelValidationResult.IsValid)
                {
                    Request.AddAlertMessage("success", "Successfully saved the settings.");
                    return Response.AsRedirect("~/administration");
                }

                return View["index", appSettings];
            };
        }
    }
}