using System;
using System.IO;
using System.Linq;
using System.Resources;
using System.Security.Claims;
using System.Threading.Tasks;
using JabbR.Infrastructure;
using JabbR.Services;
using JabbR.UploadHandlers;
using JabbR.ViewModels;
using Microsoft.AspNet.SignalR.Infrastructure;
using Microsoft.Security.Application;
using Nancy;

namespace JabbR.Nancy
{
    public class HomeModule : JabbRModule
    {
        public HomeModule(ApplicationSettings settings,
                          IJabbrConfiguration configuration,
                          IConnectionManager connectionManager,
                          IJabbrRepository jabbrRepository)
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

            Get["/status"] = _ =>
            {
                var model = new StatusViewModel();

                // Try to send a message via SignalR
                // NOTE: Ideally we'd like to actually receive a message that we send, but right now
                // that would require a full client instance. SignalR 2.1.0 plans to add a feature to
                // easily support this on the server.
                var signalrStatus = new SystemStatus { SystemName = "SignalR messaging" };
                model.Systems.Add(signalrStatus);

                try
                {
                    var hubContext = connectionManager.GetHubContext<Chat>();
                    var sendTask = (Task)hubContext.Clients.Client("doesn't exist").noMethodCalledThis();
                    sendTask.Wait();

                    signalrStatus.SetOK();
                }
                catch (Exception ex)
                {
                    signalrStatus.SetException(ex.GetBaseException());
                }

                // Try to talk to database
                var dbStatus = new SystemStatus { SystemName = "Database" };
                model.Systems.Add(dbStatus);

                try
                {
                    var roomCount = jabbrRepository.Rooms.Count();
                    dbStatus.SetOK();
                }
                catch (Exception ex)
                {
                    dbStatus.SetException(ex.GetBaseException());
                }

                // Try to talk to azure storage
                var azureStorageStatus = new SystemStatus { SystemName = "Azure Upload storage" };
                model.Systems.Add(azureStorageStatus);

                try
                {
                    if (!String.IsNullOrEmpty(settings.AzureblobStorageConnectionString))
                    {
                        var azure = new AzureBlobStorageHandler(settings);
                        UploadResult result;
                        using (var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes("test")))
                        {
                            result = azure.UploadFile("statusCheck.txt", "text/plain", stream)
                                          .Result;
                        }
                        azureStorageStatus.SetOK();
                    }
                    else
                    {
                        azureStorageStatus.StatusMessage = "Not configured";
                    }
                }
                catch (Exception ex)
                {
                    azureStorageStatus.SetException(ex.GetBaseException());
                }

                //try to talk to local storage
                var localStorageStatus = new SystemStatus { SystemName = "Local Upload storage" };
                model.Systems.Add(localStorageStatus);

                try
                {
                    if (!String.IsNullOrEmpty(settings.LocalFileSystemStoragePath) && !String.IsNullOrEmpty(settings.LocalFileSystemStorageUriPrefix))
                    {
                        var local = new LocalFileSystemStorageHandler(settings);
                        UploadResult localResult;
                        using (var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes("test")))
                        {
                            localResult = local.UploadFile("statusCheck.txt", "text/plain", stream)
                                          .Result;
                        }
                        localStorageStatus.SetOK();
                    }
                    else
                    {
                        localStorageStatus.StatusMessage = "Not configured";
                    }
                }
                catch (Exception ex)
                {
                    localStorageStatus.SetException(ex.GetBaseException());
                }

                // Force failure
                if (Context.Request.Query.fail)
                {
                    var failedSystem = new SystemStatus { SystemName = "Forced failure" };
                    failedSystem.SetException(new ApplicationException("Forced failure for test purposes"));
                    model.Systems.Add(failedSystem);
                }

                var view = View["status", model];

                if (!model.AllOK)
                {
                    return view.WithStatusCode(HttpStatusCode.InternalServerError);
                }

                return view;
            };
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