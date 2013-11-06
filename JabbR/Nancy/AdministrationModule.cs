using System;
using System.Collections.Generic;
using System.IO;
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

                try
                {
                    var settings = this.Bind<ApplicationSettings>();

                    IDictionary<string, string> errors;
                    if (ApplicationSettings.TryValidateSettings(settings, out errors))
                    {
                        settingsManager.Save(settings);
                    }
                    else
                    {
                        foreach (var error in errors)
                        {
                            this.AddValidationError(error.Key, error.Value);
                        }
                    }
                }
                catch (Exception ex)
                {
                    this.AddValidationError("_FORM", ex.Message);
                }

                if (ModelValidationResult.IsValid)
                {
                    Request.AddAlertMessage("success", LanguageResources.SettingsSaveSuccess);
                    return Response.AsRedirect("~/administration");
                }

                return View["index", applicationSettings];
            };
        }
    }
}