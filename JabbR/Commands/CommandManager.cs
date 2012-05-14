using System;
using System.Collections.Generic;
using System.ComponentModel.Composition.Hosting;
using System.Linq;
using JabbR.Models;
using JabbR.Services;

namespace JabbR.Commands
{
    public class CommandManager
    {
        private readonly string _clientId;
        private readonly string _userAgent;
        private readonly string _userId;
        private readonly string _roomName;
        private readonly INotificationService _notificationService;
        private readonly IChatService _chatService;
        private readonly ICache _cache;
        private readonly IJabbrRepository _repository;

        private static Dictionary<string, ICommand> _commandCache;
        private static readonly Lazy<IList<ICommand>> _commands = new Lazy<IList<ICommand>>(GetCommands);

        public CommandManager(string clientId,
                              string userId,
                              string roomName,
                              IChatService service,
                              IJabbrRepository repository,
                              ICache cache,
                              INotificationService notificationService)
            : this(clientId, null, userId, roomName, service, repository, cache, notificationService)
        {
        }

        public CommandManager(string clientId,
                              string userAgent,
                              string userId,
                              string roomName,
                              IChatService service,
                              IJabbrRepository repository,
                              ICache cache,
                              INotificationService notificationService)
        {
            _clientId = clientId;
            _userAgent = userAgent;
            _userId = userId;
            _roomName = roomName;
            _chatService = service;
            _repository = repository;
            _cache = cache;
            _notificationService = notificationService;
        }

        public bool TryHandleCommand(string command)
        {
            command = command.Trim();
            if (!command.StartsWith("/"))
            {
                return false;
            }

            string[] args = command.Substring(1).Split(' ');
            string commandName = args[0];

            return TryHandleCommand(commandName, args.Skip(1).ToArray());
        }

        public bool TryHandleCommand(string commandName, string[] args)
        {
            commandName = commandName.Trim();
            if (commandName.StartsWith("/"))
            {
                return false;
            }

            var context = new CommandContext
            {
                Cache = _cache,
                NotificationService = _notificationService,
                Repository = _repository,
                Service = _chatService
            };

            var callerContext = new CallerContext
            {
                ClientId = _clientId,
                UserId = _userId,
                UserAgent = _userAgent,
                RoomName = _roomName,
            };

            ICommand command;
            if (!TryMatchCommand(commandName, out command))
            {
                throw new InvalidOperationException(String.Format("'{0}' is not a valid command.", commandName));
            }

            command.Execute(context, callerContext, args);

            return true;
        }

        private bool TryMatchCommand(string commandName, out ICommand command)
        {
            if (_commandCache == null)
            {
                var commands = from c in _commands.Value
                               let commandAttribute = c.GetType()
                                                       .GetCustomAttributes(true)
                                                       .OfType<CommandAttribute>()
                                                       .FirstOrDefault()
                               where commandAttribute != null
                               select new
                               {
                                   Name = commandAttribute.CommandName,
                                   Command = c
                               };

                _commandCache = commands.ToDictionary(c => c.Name,
                                                      c => c.Command,
                                                      StringComparer.OrdinalIgnoreCase);
            }

            return _commandCache.TryGetValue(commandName, out command);
        }

        private static IList<ICommand> GetCommands()
        {
            // Use MEF to locate the content providers in this assembly
            var catalog = new AssemblyCatalog(typeof(CommandManager).Assembly);
            var compositionContainer = new CompositionContainer(catalog);
            return compositionContainer.GetExportedValues<ICommand>().ToList();
        }
    }
}