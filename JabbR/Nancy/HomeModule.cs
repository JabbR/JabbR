using System;
using System.Configuration;
using System.Linq;
using JabbR.Infrastructure;
using JabbR.Services;
using JabbR.ViewModels;
using Nancy;
using Nancy.Helpers;

namespace JabbR.Nancy
{
    public class HomeModule : JabbRModule
    {
        public HomeModule(UploadCallbackHandler uploadHandler)
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
                        Time = ConfigurationManager.AppSettings["jabbr:releaseTime"],
                        DebugMode = (bool)Context.Items["_debugMode"],
                        Version = Constants.JabbRVersion
                    };

                    return View["index", viewModel];
                }

                return Response.AsRedirect(String.Format("~/account/login?returnUrl={0}", HttpUtility.UrlEncode(Request.Path)));
            };

            Post["/upload"] = _ =>
            {
                if (Context.CurrentUser == null)
                {
                    return 403;
                }

                string roomName = Request.Form.room;
                string connectionId = Request.Form.connectionId;
                HttpFile file = Request.Files.First();

                // This blocks since we're not using nancy's async support yet
                uploadHandler.Upload(Context.CurrentUser.UserName,
                                     connectionId,
                                     roomName,
                                     file.Name,
                                     file.ContentType,
                                     file.Value).Wait();

                return 200;
            };
        }
    }
}