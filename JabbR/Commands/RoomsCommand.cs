using JabbR.Models;

namespace JabbR.Commands
{
    [Command("rooms", "")]
    public class RoomsCommand : UserCommand
    {
        public override void Execute(CommandContext context, CallerContext callerContext, ChatUser callingUser, string[] args)
        {
            context.NotificationService.ShowRooms();
        }
    }
}