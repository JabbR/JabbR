using System;
using System.Resources;

namespace JabbR.Commands
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
    public sealed class CommandAttribute : Attribute
    {
        public string CommandName { get; private set; }
        public string DescriptionResourceKey { get; set; }
        public string Arguments { get; set; }
        public string Group { get; set; }

        public string Description {
            get
            {
                if (String.IsNullOrWhiteSpace(DescriptionResourceKey))
                {
                    return String.Empty;
                }

                var resourceManager = new ResourceManager(typeof(LanguageResources));
                return resourceManager.GetString(DescriptionResourceKey);
            }
        }

        public CommandAttribute(string commandName, string descriptionResourceKey, string arguments, string group)
        {
            CommandName = commandName;
            DescriptionResourceKey = descriptionResourceKey;
            Arguments = arguments;
            Group = group;
        }
    }
}