using System;

namespace JabbR.Commands
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
    public sealed class CommandAttribute : Attribute
    {
        public string CommandName { get; private set; }
        public string Description { get; set; }
        public string Arguments { get; set; }
        public string Group { get; set; }

        public CommandAttribute(string commandName, string description, string arguments, string group)
        {
            CommandName = commandName;
            Description = description;
            Arguments = arguments;
            Group = group;
        }
    }
}