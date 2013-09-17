using System.Collections.Concurrent;
using System.Collections.Generic;
using JabbR.Models;
using JabbR.ViewModels;
using Microsoft.AspNet.SignalR.Messaging;

namespace JabbR.Services
{
    public class RecentMessageCache : IRecentMessageCache
    {
        private ConcurrentDictionary<string, MessageStore<MessageViewModel>> _cache = new ConcurrentDictionary<string, MessageStore<MessageViewModel>>();
        private static readonly List<MessageViewModel> _emptyList = new List<MessageViewModel>();

        private readonly int _numberOfMessages;

        public RecentMessageCache()
            : this(100)
        {
        }

        public RecentMessageCache(int numberOfMessages)
        {
            _numberOfMessages = numberOfMessages;
        }

        public void Add(string room, List<ChatMessage> messages)
        {
            _cache.TryAdd(room, CreateStore(messages));
        }

        public void Add(ChatMessage message)
        {
            MessageStore<MessageViewModel> store;
            if (_cache.TryGetValue(message.Room.Name, out store))
            {
                // Only cache if there's been a store created for this room already
                store.Add(new MessageViewModel(message));
            }
        }

        public IList<MessageViewModel> GetRecentMessages(string roomName)
        {
            MessageStore<MessageViewModel> store;
            if (_cache.TryGetValue(roomName, out store))
            {
                List<MessageViewModel> messages = null;
                int count = _numberOfMessages;
                ulong min = 0;

                MessageStoreResult<MessageViewModel> result;

                do
                {
                    result = store.GetMessages(min, count);

                    // Optimized
                    if (min == 0 && !result.HasMoreData)
                    {
                        // Don't create a new list if all of the data is in the first chunk
                        return result.Messages;
                    }
                    else if (messages == null)
                    {
                        // Create the list for the number of messages we want to return
                        messages = new List<MessageViewModel>(_numberOfMessages);
                    }

                    min = result.FirstMessageId + (ulong)result.Messages.Count;

                    count -= result.Messages.Count;

                    messages.AddRange(result.Messages);

                } while (count > 0 && result.HasMoreData);

                return messages;
            }

            return _emptyList;
        }

        private MessageStore<MessageViewModel> CreateStore(List<ChatMessage> messages)
        {
            var store = new MessageStore<MessageViewModel>((uint)_numberOfMessages);

            foreach (var message in messages)
            {
                store.Add(new MessageViewModel(message));
            }

            return store;
        }
    }
}