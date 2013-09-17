using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using JabbR.Models;
using JabbR.ViewModels;
using Microsoft.AspNet.SignalR.Messaging;

namespace JabbR.Services
{
    public class RecentMessageCache : IRecentMessageCache
    {
        private ConcurrentDictionary<string, RoomCache> _cache = new ConcurrentDictionary<string, RoomCache>();
        internal static readonly List<MessageViewModel> _emptyList = new List<MessageViewModel>();

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
            var roomCache = new RoomCache(_numberOfMessages);

            _cache.TryAdd(room, roomCache);

            roomCache.Populate(messages);
        }

        public void Add(ChatMessage message)
        {
            // We need to block here so that we always a
            RoomCache roomCache;
            if (_cache.TryGetValue(message.Room.Name, out roomCache))
            {
                // Only cache if there's been a store created for this room already
                roomCache.Add(message);
            }
        }

        public IList<MessageViewModel> GetRecentMessages(string roomName)
        {
            RoomCache roomCache;
            if (_cache.TryGetValue(roomName, out roomCache))
            {
                List<MessageViewModel> messages = null;
                int count = _numberOfMessages;
                ulong min = 0;

                MessageStoreResult<MessageViewModel> result;

                do
                {
                    result = roomCache.Store.GetMessages(min, count);

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

        private class RoomCache
        {
            private readonly ManualResetEventSlim _populateHandle = new ManualResetEventSlim();

            public MessageStore<MessageViewModel> Store { get; private set; }

            public RoomCache(int size)
            {
                Store = new MessageStore<MessageViewModel>((uint)size);
            }

            public void Add(ChatMessage message)
            {
                _populateHandle.Wait();

                Store.Add(new MessageViewModel(message));
            }

            public void Populate(List<ChatMessage> messages)
            {
                foreach (var message in messages)
                {
                    Store.Add(new MessageViewModel(message));
                }

                _populateHandle.Set();
            }
        }
    }

    public class NoopCache : IRecentMessageCache
    {
        public void Add(ChatMessage message)
        {
            
        }

        public void Add(string room, List<ChatMessage> messages)
        {
            
        }

        public IList<MessageViewModel> GetRecentMessages(string roomName)
        {
            return RecentMessageCache._emptyList;
        }
    }
}