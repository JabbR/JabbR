using System;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using JabbR.Infrastructure;
using JabbR.Services;
using JabbR.ViewModels;
using Nancy;

namespace JabbR.Nancy
{
    public class HomeModule : JabbRModule
    {
        public HomeModule(ApplicationSettings settings,
                          IJabbrConfiguration configuration,
                          UploadCallbackHandler uploadHandler)
        {
            Get["/"] = _ =>
            {
                if (IsAuthenticated)
                {
                    var viewModel = new SettingsViewModel
                    {
                        GoogleAnalytics = settings.GoogleAnalytics,
                        Sha = configuration.DeploymentSha,
                        Branch = configuration.DeploymentBranch,
                        Time = configuration.DeploymentTime,
                        DebugMode = (bool)Context.Items["_debugMode"],
                        Version = Constants.JabbRVersion,
                        IsAdmin = Principal.HasClaim(JabbRClaimTypes.Admin)
                    };

                    return View["index", viewModel];
                }

                if (Principal.HasPartialIdentity())
                {
                    // If the user is partially authenticated then take them to the register page
                    return Response.AsRedirect("~/account/register");
                }

                return HttpStatusCode.Unauthorized;
            };

            Get["/monitor"] = _ =>
            {
                ClaimsPrincipal principal = Principal;

                if (principal == null ||
                    !principal.HasClaim(JabbRClaimTypes.Admin))
                {
                    return 403;
                }

                return View["monitor"];
            };

            Post["/upload-file"] = _ =>
                {
                    if (!IsAuthenticated)
                    {
                        return 403;
                    }

                    string roomName = Request.Form.room;
                    string connectionId = Request.Form.connectionId;
                    string file = Request.Form.file;
                    //string fileName = "clipboard_" + Guid.NewGuid().ToString("N");
                    string fileName = Request.Form.filename;
                    string contentType = Request.Form.type;
                    byte[] binData = null;

                    var info = Regex.Match(file, @"data:(?:(?<unkown>.+?)/(?<type>.+?))?;base64,(?<data>.+)");

                    binData = Convert.FromBase64String(info.Groups["data"].Value);
                    contentType = info.Groups["type"].Value;

                    if (String.IsNullOrWhiteSpace(contentType))
                    {
                        contentType = "application/octet-stream";
                    }
                    
                    UploadFile(
                        uploadHandler,
                        Principal.GetUserId(),
                        connectionId,
                        roomName,
                        fileName,
                        contentType,
                        new MemoryStream(binData)).Wait();

                    return 200;
                };
        }

        private static Task UploadFile(UploadCallbackHandler uploadHandler, string userName, string connectionId, string roomName, string fileName, string contentType, Stream value)
        {
            return uploadHandler.Upload(userName,
                                 connectionId,
                                 roomName,
                                 fileName,
                                 contentType,
                                 value);
        }
    }
}