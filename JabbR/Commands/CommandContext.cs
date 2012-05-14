using JabbR.Models;
using JabbR.Services;

namespace JabbR.Commands
{
    public class CommandContext
    {
        public IJabbrRepository Repository { get; set; }
        public ICache Cache { get; set; }
        public IChatService Service { get; set; }
        public INotificationService NotificationService { get; set; }
    }
}