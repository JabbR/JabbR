using System;
using System.Configuration;
using JabbR.Services;
using JabbR.ViewModels;
using Nancy;

namespace JabbR.Nancy
{
    public class HomeModule : JabbRModule
    {
        public HomeModule(IAuthenticationTokenService authService)
        {
            Get["/"] = _ =>
            {
                if (Context.CurrentUser != null)
                {
                    var viewModel = new SettingsViewModel
                    {
                        GoogleAnalytics = ConfigurationManager.AppSettings["googleAnalytics"],
                        Sha = ConfigurationManager.AppSettings["googleAnalytics"],
                        Branch = ConfigurationManager.AppSettings["releaseBranch"],
                        Time = ConfigurationManager.AppSettings["releaseTime"]
                    };

                    return View["index", viewModel];
                }

                return Response.AsRedirect("~/account/login");
            };
        }
    }
}