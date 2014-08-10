using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using JabbR.ContentProviders.Core;
using JabbR.Infrastructure;
using JabbR.Services;
using Nancy;
using Nancy.ModelBinding;

namespace JabbR.Nancy
{
    public class AdministrationModule : JabbRModule
    {
        public AdministrationModule(ApplicationSettings applicationSettings,
                                    ISettingsManager settingsManager,
                                    IEnumerable<IContentProvider> contentProviders)
            : base("/administration")
        {
            Get["/"] = _ =>
            {
                if (!IsAuthenticated || !Principal.HasClaim(JabbRClaimTypes.Admin))
                {
                    return HttpStatusCode.Forbidden;
                }

                var allContentProviders = contentProviders
                    .OrderBy(provider => provider.GetType().Name)
                    .ToList();
                var model = new
                {
                    AllContentProviders = allContentProviders,
                    ApplicationSettings = applicationSettings
                };
                return View["index", model];
            };

            Post["/"] = _ =>
            {
                if (!HasValidCsrfTokenOrSecHeader)
                {
                    return HttpStatusCode.Forbidden;
                }

                if (!IsAuthenticated || !Principal.HasClaim(JabbRClaimTypes.Admin))
                {
                    return HttpStatusCode.Forbidden;
                }

                try
                {
                    var settings = this.Bind<ApplicationSettings>();

                    // filter out empty/null providers. The values posted may contain 'holes' due to removals.
                    settings.ContentProviders = this.Bind<ContentProviderSetting[]>()
                        .Where(cp => !string.IsNullOrEmpty(cp.Name))
                        .ToList();

                    var enabledContentProvidersResult = this.Bind<EnabledContentProvidersResult>();
                    
                    // we posted the enabled ones, but we store the disabled ones. Flip it around...
                    settings.DisabledContentProviders =
                        new HashSet<string>(contentProviders
                            .Select(cp => cp.GetType().Name)
                            .Where(typeName => enabledContentProvidersResult.EnabledContentProviders == null ||
                                !enabledContentProvidersResult.EnabledContentProviders.Contains(typeName))
                            .ToList());

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

        private class EnabledContentProvidersResult
        {
            public List<string> EnabledContentProviders { get; set; }
        }
    }
}