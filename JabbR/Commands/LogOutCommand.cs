using JabbR.Models;

namespace JabbR.Commands
{
    [Command("logout", "Logout from this client (chat cookie will be removed).", "", "user")]
    public class LogOutCommand : UserCommand
    {
        public override void Execute(CommandContext context, CallerContext callerContext, ChatUser callingUser, string[] args)
        {
            context.NotificationService.LogOut(callingUser, callerContext.ClientId);
        }
    }
}