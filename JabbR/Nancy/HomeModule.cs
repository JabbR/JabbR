using System;
using System.Configuration;
using JabbR.ViewModels;
using Nancy;
using Nancy.Helpers;

namespace JabbR.Nancy
{
    public class HomeModule : JabbRModule
    {
        public HomeModule()
        {
            Get["/"] = _ =>
            {
                if (Context.CurrentUser != null)
                {
                    var viewModel = new SettingsViewModel
                    {
                        GoogleAnalytics = ConfigurationManager.AppSettings["jabbr:googleAnalytics"],
                        Sha = ConfigurationManager.AppSettings["jabbr:releaseSha"],
                        Branch = ConfigurationManager.AppSettings["jabbr:releaseBranch"],
                        Time = ConfigurationManager.AppSettings["jabbr:releaseTime"]
                    };

                    return View["index", viewModel];
                }

                return Response.AsRedirect(String.Format("~/account/login?returnUrl={0}", HttpUtility.UrlEncode(Request.Path)));
            };
        }
    }
}