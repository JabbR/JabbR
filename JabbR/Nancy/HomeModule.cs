using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Resources;
using System.Security.Claims;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using JabbR.Infrastructure;
using JabbR.Services;
using JabbR.ViewModels;

using Microsoft.Security.Application;

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
                        IsAdmin = Principal.HasClaim(JabbRClaimTypes.Admin),
                        ClientLanguageResources = BuildClientResources()
                    };

                    return View["index", viewModel];
                }

                if (Principal != null && Principal.HasPartialIdentity())
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
                    return HttpStatusCode.Forbidden;
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

        private static string BuildClientResources()
        {
            var resourcesToEmbed = new string[]
            {
                "Content_HeaderAndToggle",
                "Chat_YouEnteredRoom",
                "Chat_UserLockedRoom",
                "Chat_RoomNowLocked",
                "Chat_RoomNowClosed",
                "Chat_RoomNowOpen",
                "Chat_UserEnteredRoom",
                "Chat_UserNameChanged",
                "Chat_UserGravatarChanged",
                "Chat_YouGrantedRoomAccess",
                "Chat_UserGrantedRoomAccess",
                "Chat_YourRoomAccessRevoked",
                "Chat_YouRevokedUserRoomAccess",
                "Chat_UserGrantedRoomOwnership",
                "Chat_UserRoomOwnershipRevoked",
                "Chat_YouGrantedRoomOwnership",
                "Chat_YourRoomOwnershipRevoked",
                "Chat_YourGravatarChanged",
                "Chat_YourPasswordSet",
                "Chat_YourPasswordChanged",
                "Chat_YouAreAfk",
                "Chat_YouAreAfkNote",
                "Chat_YourNoteSet",
                "Chat_YourNoteCleared",
                "Chat_UserIsAfk",
                "Chat_UserIsAfkNote",
                "Chat_UserNoteSet",
                "Chat_UserNoteCleared",
                "Chat_YouSetRoomTopic",
                "Chat_YouClearedRoomTopic",
                "Chat_UserSetRoomTopic",
                "Chat_UserClearedRoomTopic",
                "Chat_YouSetRoomWelcome",
                "Chat_YouClearedRoomWelcome",
                "Chat_YouSetFlag",
                "Chat_YouClearedFlag",
                "Chat_UserSetFlag",
                "Chat_UserClearedFlag",
                "Chat_YourNameChanged",
                "Chat_UserPerformsAction",
                "Chat_PrivateMessage",
                "Chat_UserInvitedYouToRoom",
                "Chat_YouInvitedUserToRoom",
                "Chat_UserNudgedYou",
                "Chat_UserNudgedRoom",
                "Chat_UserNudgedUser",
                "Chat_UserLeftRoom",
                "Chat_YouKickedFromRoom",
                "Chat_NoRoomsAvailable",
                "Chat_RoomUsersHeader",
                "Chat_RoomUsersEmpty",
                "Chat_RoomSearchEmpty",
                "Chat_RoomSearchResults",
                "Chat_RoomNotPrivateAllowed",
                "Chat_RoomPrivateNoUsersAllowed",
                "Chat_RoomPrivateUsersAllowedResults",
                "Chat_UserNotInRooms",
                "Chat_UserInRooms",
                "Chat_UserOwnsNoRooms",
                "Chat_UserOwnsRooms",
                "Chat_UserAdminAllowed",
                "Chat_UserAdminRevoked",
                "Chat_YouAdminAllowed",
                "Chat_YouAdminRevoked",
                "Chat_AdminBroadcast",
                "Chat_CannotSendLobby",
                "Chat_InitialMessages",
                "Chat_UserOwnerHeader",
                "Chat_UserHeader",
                "Content_DisabledMessage",
                "Chat_DefaultTopic",
                "Client_ConnectedStatus",
                "Client_Transport",
                "Client_Uploading",
                "Client_Rooms",
                "Client_OtherRooms",
                "Chat_ExpandHiddenMessages",
                "Chat_CollapseHiddenMessages",
                "Client_Connected",
                "Client_Reconnecting",
                "Client_Disconnected",
                "Client_AdminTag",
                "Client_OccupantsZero",
                "Client_OccupantsOne",
                "Client_OccupantsMany",
                "LoadingMessage",
                "Client_LoadMore",
                "Client_UploadingFromClipboard"
            };

            var resourceManager = new ResourceManager(typeof(LanguageResources));
            return String.Join(",", resourcesToEmbed.Select(e => string.Format("'{0}': {1}", e, Encoder.JavaScriptEncode(resourceManager.GetString(e)))));
        }
    }
}