using System;
using JabbR.Models;
using Microsoft.AspNet.SignalR;

namespace JabbR.Commands
{
    [Command("create", "Create_CommandInfo", "room", "room")]
    public class CreateCommand : UserCommand
    {
        public override void Execute(CommandContext context, CallerContext callerContext, ChatUser callingUser, string[] args)
        {
            if (args.Length > 1)
            {
                throw new HubException(LanguageResources.RoomInvalidNameSpaces);
            }

            if (args.Length == 0)
            {
                throw new HubException(LanguageResources.RoomRequired);
            }

            string roomName = args[0];
            if (String.IsNullOrWhiteSpace(roomName))
            {
                throw new HubException(LanguageResources.RoomRequired);
            }

            ChatRoom room = context.Repository.GetRoomByName(roomName);

            if (room != null)
            {
                if (!room.Closed)
                {
                    throw new HubException(String.Format(LanguageResources.RoomExists, roomName));
                }
                else
                {
                    throw new HubException(String.Format(LanguageResources.RoomExistsButClosed, roomName));
                }
            }

            // Create the room, then join it
            room = context.Service.AddRoom(callingUser, roomName);

            context.Service.JoinRoom(callingUser, room, null);

            context.Repository.CommitChanges();

            context.NotificationService.JoinRoom(callingUser, room);
        }
    }
}