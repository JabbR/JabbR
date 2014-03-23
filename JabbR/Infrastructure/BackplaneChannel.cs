using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

using Microsoft.AspNet.SignalR.Infrastructure;
using Microsoft.AspNet.SignalR.Json;
using Microsoft.AspNet.SignalR.Messaging;

using Newtonsoft.Json;

namespace JabbR.Infrastructure
{
    public class BackplaneChannel : ISubscriber, IBackplaneChannel
    {
        private const string BackplaneSignal = "__BACKPLANE__";

        private readonly List<string> _signals = new List<string>{ BackplaneSignal };

        private readonly JsonSerializer _jsonSerializer;

        private readonly IServerIdManager _serverIdManager;

        private readonly IMessageBus _messageBus;

        private readonly IEnumerable<IBackplaneMethodResolver> _methodResolvers;

        private readonly ConcurrentDictionary<string, BackplaneChannelRegistration> _subscriptions = new ConcurrentDictionary<string, BackplaneChannelRegistration>();

        public BackplaneChannel(JsonSerializer jsonSerializer, IServerIdManager serverIdManager, IMessageBus messageBus, IEnumerable<IBackplaneMethodResolver> methodResolvers)
        {
            _jsonSerializer = jsonSerializer;
            _serverIdManager = serverIdManager;
            _messageBus = messageBus;
            _methodResolvers = methodResolvers;
        }

        public void Subscribe()
        {
            _messageBus.Subscribe(this, cursor: null, callback: HandleMessages, maxMessages: 10, state: null);
        }

        public void Subscribe<T>(T instance) where T : IDisposable
        {
            Subscribe(instance, channelName: null);
        }

        public void Subscribe<T>(T instance, string channelName) where T : IDisposable
        {
            if (string.IsNullOrEmpty(channelName))
            {
                channelName = typeof(T).FullName;
            }

            var channelRegistration = new BackplaneChannelRegistration
            {
                Name = channelName,
                Instance = instance,
                Type = typeof(T)
            };

            // overwrite the last registration
            _subscriptions.AddOrUpdate(channelName, channelRegistration, (key, reg) => channelRegistration);
        }

        public void Unsubscribe<T>(T instance) where T : IDisposable
        {
            foreach (var key in _subscriptions.Keys)
            {
                if (_subscriptions[key].Instance == (object)instance)
                {
                    BackplaneChannelRegistration channelRegistration;
                    _subscriptions.TryRemove(key, out channelRegistration);
                }
            }
        }

        public void Unsubscribe(string channelName)
        {
            BackplaneChannelRegistration channelRegistration;
            _subscriptions.TryRemove(channelName, out channelRegistration);
        }

        public void Invoke<T>(string methodName, object[] arguments)
        {
            Invoke(typeof(T).FullName, methodName, arguments);
        }

        public void Invoke(string channelName, string methodName, object[] arguments)
        {
            var message = new BackplaneChannelMessage
            {
                ChannelName = channelName,
                MethodName = methodName,
                Arguments = arguments.Select(e => _jsonSerializer.Stringify(e)).ToArray()
            };
            SendMessage(message);
        }

        private Task<bool> HandleMessages(MessageResult result, object state)
        {
            result.Messages.Enumerate<object>(
                m => _signals.Any(s => s == m.Key),
                (s, m) =>
                {
                    // ignore the message if we sent it in the first place
                    if (m.Source == _serverIdManager.ServerId)
                    {
                        return;
                    }

                    // find a subscription
                    var message = _jsonSerializer.Parse<BackplaneChannelMessage>(m.Value, m.Encoding);
                    InvokeMethod(message.ChannelName, message.MethodName, message.Arguments);
                },
                state: null);

            return Task.FromResult(true);
        }

        private void InvokeMethod(string channelName, string methodName, string[] arguments)
        {
            BackplaneChannelRegistration subscription;
            if (!_subscriptions.TryGetValue(channelName, out subscription))
            {
                // if we can't find the channel, do nothing
                return;
            }

            BackplaneMethodRegistration method = null;
            int argumentCount = arguments != null ? arguments.Length : 0;
            if (_methodResolvers.FirstOrDefault(p => p.TryGetMethod(subscription, methodName, argumentCount, out method)) == null)
            {
                return;
            }

            // deserialize parameters
            var deserializedArguments = new object[method.ArgumentTypes.Length];

            for (var idx = 0; idx < arguments.Length; idx++)
            {
                using (var paramReader = new StringReader(arguments[idx]))
                {
                    deserializedArguments[idx] = _jsonSerializer.Deserialize(paramReader, method.ArgumentTypes[idx]);
                }
            }

            method.Invoker(subscription, deserializedArguments);
        }

        private void SendMessage<T>(T message)
        {
            _messageBus.Publish(new Message(_serverIdManager.ServerId, BackplaneSignal, _jsonSerializer.Stringify(message)));
        }

        event Action<ISubscriber, string> ISubscriber.EventKeyAdded
        {
            add { }
            remove { }
        }

        event Action<ISubscriber, string> ISubscriber.EventKeyRemoved
        {
            add { }
            remove { }
        }

        IList<string> ISubscriber.EventKeys
        {
            get
            {
                return _signals;
            }
        }

        string ISubscriber.Identity
        {
            get
            {
                return _serverIdManager.ServerId;
            }
        }

        Subscription ISubscriber.Subscription { get; set; }

        Action<TextWriter> ISubscriber.WriteCursor { get; set; }

        private class BackplaneChannelMessage
        {
            public string ChannelName { get; set; }
            public string MethodName { get; set; }
            public string[] Arguments { get; set; }
        }
    }
}