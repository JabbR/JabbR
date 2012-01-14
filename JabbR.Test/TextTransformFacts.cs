using System.Collections.Generic;
using System.Text.RegularExpressions;
using JabbR.Infrastructure;
using JabbR.Models;
using Xunit;

namespace JabbR.Test
{
    public class TextTransformFacts
    {
        public class ConvertHashtagsToRoomLinksFacts
        {
            private Regex HashtagRegex()
            {
                return new Regex(TextTransform.HashTagPattern);
            }

            public IJabbrRepository CreateRoomRepository()
            {
                var repository = new InMemoryRepository();
                var room = new ChatRoom() { Name = "hashtag" };
                var user = new ChatUser() { Name = "testhashtaguser" };
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
            public void HashtagRegexMatchesHashtagWithDashString()
            {
                Regex hashtagRegex = HashtagRegex();

                var result = hashtagRegex.IsMatch("#dash-tag");

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

        public class ConvertUrlsToLinksFacts
        {
            [Fact]
            public void UrlWithoutHttpIsTransformed()
            {
                //arrange
                var message = "message www.jabbr.net continues on";
                HashSet<string> extractedUrls;

                //act
                var result = TextTransform.TransformAndExtractUrls(message, out extractedUrls);

                //assert
                Assert.Equal("message <a rel=\"nofollow external\" target=\"_blank\" href=\"http://www.jabbr.net\" title=\"www.jabbr.net\">www.jabbr.net</a> continues on", result);
            }

            [Fact]
            public void UrlWithHttpIsTransformed()
            {
                //arrange
                var message = "message http://www.jabbr.net continues on";
                HashSet<string> extractedUrls;

                //act
                var result = TextTransform.TransformAndExtractUrls(message, out extractedUrls);

                //assert
                Assert.Equal("message <a rel=\"nofollow external\" target=\"_blank\" href=\"http://www.jabbr.net\" title=\"http://www.jabbr.net\">http://www.jabbr.net</a> continues on", result);
            }

            [Fact]
            public void UrlWithHttpsIsTransformed()
            {
                //arrange
                var message = "message https://www.jabbr.net continues on";
                HashSet<string> extractedUrls;

                //act
                var result = TextTransform.TransformAndExtractUrls(message, out extractedUrls);

                //assert
                Assert.Equal("message <a rel=\"nofollow external\" target=\"_blank\" href=\"https://www.jabbr.net\" title=\"https://www.jabbr.net\">https://www.jabbr.net</a> continues on", result);
            }
        }
    }
}