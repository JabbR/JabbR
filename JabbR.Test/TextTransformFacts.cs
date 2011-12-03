using System.Text.RegularExpressions;
using JabbR.Infrastructure;
using JabbR.Models;
using Xunit;

namespace JabbR.Test {

    public class TextTransformFacts 
    {

        public class ConvertHashtagsToRoomLinksFacts 
        {

            public Regex HashtagRegex() 
            {
                return new Regex(TextTransform.HashTagPattern);
            }

            public IJabbrRepository CreateRoomRepository() 
            {
                var repository = new InMemoryRepository();
                var room = new ChatRoom() {Name = "hashtag"};
                var user = new ChatUser() {Name = "testhashtaguser"};
                repository.Add(room);
                room.Users.Add(user);
                user.Rooms.Add(room);

                return repository;
            }

            [Fact]
            public void HashtagRegexMatchesHashtagString() 
            {
                Regex hashtagRegex = HashtagRegex();

                var result = hashtagRegex.IsMatch("#hashtag");

                Assert.True(result);
            }

            [Fact]
            public void HashtagRegexMatchesHashtagInSubstring() 
            {
                Regex hashtagRegex = HashtagRegex();

                var result = hashtagRegex.IsMatch("this #hashtag is in the middle of the string");

                Assert.True(result);
            }

            [Fact]
            public void HashtagRegexDoesNotMatchInStringWithoutHashtag() 
            {
                Regex hashtagRegex = HashtagRegex();

                var result = hashtagRegex.IsMatch("this hashtag is in the middle of the string");

                Assert.False(result);
            }

            [Fact]
            public void HashtagRegexParts() 
            {
                Regex hashtagRegex = HashtagRegex();
                var match = hashtagRegex.Match("#hashtag");

                Assert.Equal("#hashtag", match.Value);
                Assert.Equal("hashtag", match.Groups[1].Value);

            }

            [Fact]
            public void StringWithHashtagModifiesHashtagToRoomLink() 
            {
                IJabbrRepository repository = CreateRoomRepository();
                string expected = "<a href=\"#/rooms/hashtag\" title=\"#hashtag\">#hashtag</a>";

                TextTransform transform = new TextTransform(repository);
                string result = transform.Parse("#hashtag");

                Assert.Equal(expected, result);
            }

            [Fact]
            public void StringWithHashtagButRoomDoesntExistDoesNotModifyMessage() 
            {
                IJabbrRepository repository = CreateRoomRepository();

                TextTransform transform = new TextTransform(repository);
                string result = transform.Parse("#thisdoesnotexist");

                Assert.Equal("#thisdoesnotexist", result);
            }

        }
    }
}