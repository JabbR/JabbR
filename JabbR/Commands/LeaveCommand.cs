using JabbR.Models;

namespace JabbR.Commands
{
    [Command("leave", "Leave the current room. Use [room] to leave a specific room.", "[room]", "room")]
    public class LeaveCommand : UserCommand
    {
        public override void Execute(CommandContext context, CallerContext callerContext, ChatUser callingUser, string[] args)
        {
            ChatRoom room = null;
            if (args.Length  == 0)
            {
                room = context.Repository.VerifyUserRoom(context.Cache, callingUser, callerContext.RoomName);                
            }
            else
            {
                string roomName = args[0];

                room = context.Repository.VerifyRoom(roomName, mustBeOpen: false);
            }

            context.Service.LeaveRoom(callingUser, room);

            context.NotificationService.LeaveRoom(callingUser, room);

            context.Repository.CommitChanges();
        }
    }
}