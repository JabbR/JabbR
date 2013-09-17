using System.Collections.Generic;
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

            var messages = cache.GetRecentMessages("room");

            Assert.Equal(10, messages.Count);
            
            // The impl of the message store allocates bucket sizes optimized for the GC so it's not exact
            for (int i = 64, j = 0; i <= 73; i++, j++)
            {
                Assert.Equal("Hello_" + i, messages[j].Content);
            }
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
