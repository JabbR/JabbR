using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using JabbR.Models;
using JabbR.ViewModels;

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

            public void Populate(List<ChatMessage> messages)
            {
                foreach (var message in messages)
                {
                    _store.AddLast(new MessageViewModel(message));
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