using JabbR.Infrastructure;
using Xunit;

namespace JabbR.Tests
{
    public class MentionExtractorFacts
    {
        [Fact]
        public void FirstCharacter()
        {
            var users = MentionExtractor.ExtractMentions("@foo");

            Assert.Equal(1, users.Count);
            Assert.Equal("foo", users[0]);
        }

        [Fact]
        public void TrailingSpace()
        {
            var users = MentionExtractor.ExtractMentions("@foo ");

            Assert.Equal(1, users.Count);
            Assert.Equal("foo", users[0]);
        }

        [Fact]
        public void Multiple()
        {
            var users = MentionExtractor.ExtractMentions("@foo @bar");

            Assert.Equal(2, users.Count);
            Assert.Equal("foo", users[0]);
            Assert.Equal("bar", users[1]);
        }

        [Fact]
        public void Nothing()
        {
            var users = MentionExtractor.ExtractMentions("@@@@@");

            Assert.Equal(0, users.Count);
        }
    }
}
