using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using JabbR.Models;
using JabbR.ViewModels;

namespace JabbR.Services
{
    public class NoopCache : IRecentMessageCache
    {
        public void Add(ChatMessage message)
        {
            
        }

        public void Add(string room, ICollection<MessageViewModel> messages)
        {
            
        }

        public ICollection<MessageViewModel> GetRecentMessages(string roomName)
        {
            return RecentMessageCache._emptyList;
        }
    }

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

        public void Add(string room, ICollection<MessageViewModel> messages)
        {
            var roomCache = new RoomCache(_numberOfMessages);

            if (_cache.TryAdd(room, roomCache))
            {
                roomCache.Populate(messages);
            }
        }

        public void Add(ChatMessage message)
        {
            RoomCache roomCache;
            if (_cache.TryGetValue(message.Room.Name, out roomCache))
            {
                // Only cache if there's been a store created for this room already
                roomCache.Add(message);
            }
        }

        public ICollection<MessageViewModel> GetRecentMessages(string roomName)
        {
            RoomCache roomCache;
            if (_cache.TryGetValue(roomName, out roomCache))
            {
                return roomCache.GetMessages();
            }

            return _emptyList;
        }

        private class RoomCache
        {
            private readonly ManualResetEventSlim _populateHandle = new ManualResetEventSlim();
            private readonly LinkedList<MessageViewModel> _store;

            private readonly int _size;

            public RoomCache(int size)
            {
                _size = size;
                _store = new LinkedList<MessageViewModel>();
            }

            public void Add(ChatMessage message)
            {
                // We need to block here so that we always a
                _populateHandle.Wait();

                lock (_store)
                {
                    if (_store.Count >= _size)
                    {
                        _store.RemoveFirst();
                    }

                    _store.AddLast(new MessageViewModel(message));
                }
            }

            public void Populate(ICollection<MessageViewModel> messages)
            {
                foreach (var message in messages)
                {
                    _store.AddLast(message);
                }

                _populateHandle.Set();
            }

            public ICollection<MessageViewModel> GetMessages()
            {
                return _store;
            }
        }
    }
}