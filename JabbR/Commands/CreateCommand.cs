using System;
using System.Web;
using JabbR.Models;

namespace JabbR.Commands
{
    [Command("create", "")]
    public class CreateCommand : UserCommand
    {
        public override void Execute(CommandContext context, CallerContext callerContext, ChatUser callingUser, string[] args)
        {
            if (args.Length > 1)
            {
                throw new InvalidOperationException("Room name cannot contain spaces.");
            }

            if (args.Length == 0)
            {
                throw new InvalidOperationException("No room specified.");
            }

            string roomName = HttpUtility.HtmlDecode(args[0]);
            if (String.IsNullOrWhiteSpace(roomName))
            {
                throw new InvalidOperationException("No room specified.");
            }

            ChatRoom room = context.Repository.GetRoomByName(roomName);

            if (room != null)
            {
                throw new InvalidOperationException(String.Format("The room '{0}' already exists{1}",
                    roomName,
                    room.Closed ? " but it's closed" : String.Empty));
            }

            // Create the room, then join it
            room = context.Service.AddRoom(callingUser, roomName);

            context.Service.JoinRoom(callingUser, room, null);

            context.Repository.CommitChanges();

            context.NotificationService.JoinRoom(callingUser, room);
        }
    }
}