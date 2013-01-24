using System;
using System.Configuration;
using JabbR.Infrastructure;
using JabbR.Services;
using JabbR.ViewModels;
using Nancy;

namespace JabbR.Nancy
{
    public class HomeModule : NancyModule
    {
        public HomeModule(IAuthenticationService authService, IApplicationSettings settings)
        {
            Get["/"] = _ =>
            {
                string userToken;
                if (Request.Cookies.TryGetValue(Constants.UserTokenCookie, out userToken) &&
                    !String.IsNullOrEmpty(userToken) &&
                    authService.IsUserAuthenticated(userToken))
                {

                    var viewModel = new SettingsViewModel
                    {
                        GoogleAnalytics = ConfigurationManager.AppSettings["googleAnalytics"],
                        Sha = ConfigurationManager.AppSettings["googleAnalytics"],
                        Branch = ConfigurationManager.AppSettings["releaseBranch"],
                        Time = ConfigurationManager.AppSettings["releaseTime"],
                        AuthMode = settings.AuthenticationMode
                    };

                    return View["index", viewModel];
                }

                return Response.AsRedirect("~/account/login");
            };
        }
    }
}