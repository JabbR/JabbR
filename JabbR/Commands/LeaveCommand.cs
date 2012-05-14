using System.Web;
using JabbR.Models;

namespace JabbR.Commands
{
    [Command("leave", "")]
    public class LeaveCommand : UserCommand
    {
        public override void Execute(CommandContext context, CallerContext callerContext, ChatUser callingUser, string[] args)
        {
            ChatRoom room = null;
            if (args.Length > 1)
            {
                string roomName = HttpUtility.HtmlDecode(args[1]);

                room = context.Repository.VerifyRoom(roomName);
            }
            else
            {
                room = context.Repository.VerifyUserRoom(context.Cache, callingUser, callerContext.RoomName);
            }

            context.Service.LeaveRoom(callingUser, room);

            context.NotificationService.LeaveRoom(callingUser, room);

            context.Repository.CommitChanges();
        }
    }
}