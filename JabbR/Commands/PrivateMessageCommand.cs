using System;
using System.Linq;
using JabbR.Models;

namespace JabbR.Commands
{
    using System.Collections.Generic;

    [Command("msg", "Msg_CommandInfo", "@user message", "user")]
    public class PrivateMessageCommand : UserCommand
    {
        public override void Execute(CommandContext context, CallerContext callerContext, ChatUser callingUser, string[] args)
        {
            if (args.Length == 0 || String.IsNullOrWhiteSpace(args[0]))
            {
                throw new InvalidOperationException(LanguageResources.Msg_UserRequired);
            }

            var toUserName = args[0];
            ChatUser toUser = context.Repository.VerifyUser(toUserName);

            if (toUser == callingUser)
            {
                throw new InvalidOperationException(LanguageResources.Msg_CannotMsgSelf);
            }

            string messageText = String.Join(" ", args.Skip(1)).Trim();

            if (String.IsNullOrEmpty(messageText))
            {
                throw new InvalidOperationException(String.Format(LanguageResources.Msg_MessageRequired, toUser.Name));
            }

            // sort members in room
            var roomUsers = new List<string> { callingUser.Id, toUser.Id };
            string roomName = "PM_" + string.Join("_", roomUsers.OrderBy(e => e).ToArray());

            var privateMessageRoom = context.Repository.GetRoomByName(roomName);
            if (privateMessageRoom == null)
            {
                privateMessageRoom = new ChatRoom
                {
                    Name = roomName,
                    Creator = null,
                    OwnersCanAllow = false,
                    UsersCanAllow = false,
                    RoomType = RoomType.PrivateMessage
                };
                privateMessageRoom.AllowedUsers.Add(callingUser);
                privateMessageRoom.AllowedUsers.Add(toUser);

                context.Repository.Add(privateMessageRoom);
                context.Repository.CommitChanges();
            }

            if (!context.Repository.IsUserInRoom(context.Cache, callingUser, privateMessageRoom))
            {
                // Join the room
                context.Service.JoinRoom(callingUser, privateMessageRoom, null);
                context.Repository.CommitChanges();
            }

            context.NotificationService.JoinRoom(callingUser, privateMessageRoom);
            ((Chat)context.NotificationService).Send(messageText, privateMessageRoom.Name);
        }
    }
}