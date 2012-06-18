using JabbR.Models;

namespace JabbR.Commands
{
    [Command("update", "")]
    public class UpdateCommand : AdminCommand
    {        
        public override void ExecuteAdminOperation(CommandContext context, CallerContext callerContext, ChatUser callingUser, string[] args)
        {
            context.NotificationService.ForceUpdate();
        }
    }
}