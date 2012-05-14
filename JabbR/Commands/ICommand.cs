using System.ComponentModel.Composition;

namespace JabbR.Commands
{
    [InheritedExport]
    public interface ICommand
    {
        void Execute(CommandContext context, CallerContext callerContext, string[] args);
    }
}