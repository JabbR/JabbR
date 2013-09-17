using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using JabbR.Models;
using JabbR.Services;
using Xunit;

namespace JabbR.Tests
{
    public class RecentMessageCacheFacts
    {
        [Fact]
        public void GetRecentMessagesReturnsTheLastNMessages()
        {
            var cache = new RecentMessageCache(10);

            // Prime the cache
            cache.Add("room", new List<ChatMessage>());

            for (int i = 0; i < 100; i++)
            {
                cache.Add(MakeMessage("dfowler", "room", "Hello_" + i));
            }

            var messages = cache.GetRecentMessages("room").ToList();

            Assert.Equal(10, messages.Count);

            // The impl of the message store allocates bucket sizes optimized for the GC so it's not exact
            for (int i = 90, j = 0; i <= 99; i++, j++)
            {
                Assert.Equal("Hello_" + i, messages[j].Content);
            }
        }

        [Fact]
        public void ThreadSafety()
        {
            var cache = new RecentMessageCache(10);
            var initial = new List<ChatMessage>();
            var wh = new ManualResetEventSlim();

            for (int i = 0; i < 5; i++)
            {
                initial.Add(MakeMessage("dfowler", "room", "Hello_" + i));
            }

            Task.Run(() =>
            {
                Thread.Sleep(100);

                for (int i = 0; i < 5; i++)
                {
                    cache.Add(MakeMessage("john", "room", "john_" + i));
                }

                wh.Set();
            });

            cache.Add("room", initial);

            wh.Wait();

            var messages = cache.GetRecentMessages("room").ToList();

            Assert.Equal(10, messages.Count);

            Assert.Equal("john_0", messages[5].Content);
            Assert.Equal("john_1", messages[6].Content);
            Assert.Equal("john_2", messages[7].Content);
            Assert.Equal("john_3", messages[8].Content);
            Assert.Equal("john_4", messages[9].Content);
        }

        private static ChatMessage MakeMessage(string user, string room, string content)
        {
            return new ChatMessage
            {
                Content = content,
                User = new ChatUser
                {
                    Name = user
                },
                Room = new ChatRoom
                {
                    Name = room
                }
            };
        }
    }
}
