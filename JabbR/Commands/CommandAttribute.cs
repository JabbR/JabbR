using System;

namespace JabbR.Commands
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
    public sealed class CommandAttribute : Attribute
    {
        public string CommandName { get; private set; }
        public string Description { get; set; }

        public CommandAttribute(string commandName, string description)
        {
            CommandName = commandName;
            Description = description;
        }
    }
}