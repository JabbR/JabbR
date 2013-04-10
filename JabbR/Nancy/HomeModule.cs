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
    using System.IO;
    using System.Text.RegularExpressions;

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

                UploadFile(
                    uploadHandler, 
                    Context.CurrentUser.UserName, 
                    connectionId, 
                    roomName, 
                    file.Name, 
                    file.ContentType, 
                    file.Value);

                return 200;
            };

            Post["/upload-clipboard"] = _ =>
                {
                    if (Context.CurrentUser == null)
                    {
                        return 403;
                    }

                    string roomName = Request.Form.room;
                    string connectionId = Request.Form.connectionId;
                    string file = Request.Form.file;
                    string fileName = "clipboard_" + Guid.NewGuid().ToString("N");
                    string contentType = "image/jpeg";

                    var info = Regex.Match(file, @"data:image/(?<type>.+?);base64,(?<data>.+)");

                    var binData = Convert.FromBase64String(info.Groups["data"].Value);
                    contentType = info.Groups["type"].Value;

                    fileName = fileName + "." + contentType.Substring(contentType.IndexOf("/") + 1);

                    UploadFile(
                        uploadHandler, 
                        Context.CurrentUser.UserName, 
                        connectionId, 
                        roomName, 
                        fileName, 
                        contentType, 
                        new MemoryStream(binData));

                    return 200;
                };
        }

        private static void UploadFile(UploadCallbackHandler uploadHandler, string userName, string connectionId, string roomName, string fileName, string contentType, Stream value)
        {
            uploadHandler.Upload(userName,
                                 connectionId,
                                 roomName,
                                 fileName,
                                 contentType,
                                 value).Wait();
        }
    }
}