namespace JabbR.Commands
{
    [Command("?", "Show this list of commands.", "", "shortcut")]
    public class HelpCommand : ICommand
    {
        public void Execute(CommandContext context, CallerContext callerContext, string[] args)
        {
            context.NotificationService.ShowHelp();
        }
    }
}