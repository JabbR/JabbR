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
            public void HashtagRegexMatchesHashtagAtEndOfSentence()
            {
                Regex hashtagRegex = HashtagRegex();

                var result = hashtagRegex.IsMatch("this hashtag is at the end of a sentance, #hashtag.");

                Assert.True(result);
            }

            [Fact]
            public void HashtagRegexMatchDoesNotIncludePeriod()
            {
                Regex hashtagRegex = HashtagRegex();

                var result = hashtagRegex.Matches("this hashtag is at the end of a sentance, #hashtag.");

                Assert.Equal(result.Count, 1);
                Assert.Equal(result[0].Value,"#hashtag");
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
            public void UrlWithCapitalizedHttpIsTransformed()
            {
                //arrange
                var message = "message HTTP://www.jabbr.net continues on";
                HashSet<string> extractedUrls;

                //act
                var result = TextTransform.TransformAndExtractUrls(message, out extractedUrls);

                //assert
                Assert.Equal("message <a rel=\"nofollow external\" target=\"_blank\" href=\"HTTP://www.jabbr.net\" title=\"HTTP://www.jabbr.net\">HTTP://www.jabbr.net</a> continues on", result);
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

            [Fact]
            public void UrlWithParenthesesIsTransformed()
            {
                //arrange
                var message = "message http://www.jabbr.net/jab(br) continues on";
                HashSet<string> extractedUrls;

                //act
                var result = TextTransform.TransformAndExtractUrls(message, out extractedUrls);

                //assert
                Assert.Equal("message <a rel=\"nofollow external\" target=\"_blank\" href=\"http://www.jabbr.net/jab(br)\" title=\"http://www.jabbr.net/jab(br)\">http://www.jabbr.net/jab(br)</a> continues on", result);
            }

            [Fact]
            public void UrlWithUnicodeIsTransformed()
            { 
                //arrange
                var message = "message http://➡.ws/䨹 continues on";
                HashSet<string> extractedUrls;

                //act
                var result = TextTransform.TransformAndExtractUrls(message, out extractedUrls);

                //assert
                Assert.Equal("message <a rel=\"nofollow external\" target=\"_blank\" href=\"http://➡.ws/䨹\" title=\"http://➡.ws/䨹\">http://➡.ws/䨹</a> continues on", result);
            }

            [Fact]
            public void UrlWithEllipsisIsTransformed() {
                //arrange
                var message = "message https://github.com/NuGet/NuGetGallery/compare/345ea25491...90a05bc3e0 continues on";
                HashSet<string> extractedUrls;

                //act
                var result = TextTransform.TransformAndExtractUrls(message, out extractedUrls);

                //assert
                Assert.Equal("message <a rel=\"nofollow external\" target=\"_blank\" href=\"https://github.com/NuGet/NuGetGallery/compare/345ea25491...90a05bc3e0\" title=\"https://github.com/NuGet/NuGetGallery/compare/345ea25491...90a05bc3e0\">https://github.com/NuGet/NuGetGallery/compare/345ea25491...90a05bc3e0</a> continues on", result);
            }
        }
    }
}