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
                Assert.Equal(result[0].Value, "#hashtag");
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
            public void UrlWithSingleTrailingParanthesisMatchesCloseBracketAsText()
            {
                // Arrange
                var message = "(message http://www.jabbr.net/) doesn't match the outside brackets";
                HashSet<string> extractedUrls;

                // Act
                var result = TextTransform.TransformAndExtractUrls(message, out extractedUrls);

                // Assert
                Assert.Equal("(message <a rel=\"nofollow external\" target=\"_blank\" href=\"http://www.jabbr.net/\" title=\"http://www.jabbr.net/\">http://www.jabbr.net/</a>) doesn't match the outside brackets", result);

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
                Assert.Equal("message <a rel=\"nofollow external\" target=\"_blank\" href=\"http://&#10145;.ws/&#19001;\" title=\"http://➡.ws/䨹\">http://➡.ws/䨹</a> continues on", result);
            }

            [Fact]
            public void UrlWithEllipsisIsTransformed()
            {
                //arrange
                var message = "message https://github.com/NuGet/NuGetGallery/compare/345ea25491...90a05bc3e0 continues on";
                HashSet<string> extractedUrls;

                //act
                var result = TextTransform.TransformAndExtractUrls(message, out extractedUrls);

                //assert
                Assert.Equal("message <a rel=\"nofollow external\" target=\"_blank\" href=\"https://github.com/NuGet/NuGetGallery/compare/345ea25491...90a05bc3e0\" title=\"https://github.com/NuGet/NuGetGallery/compare/345ea25491...90a05bc3e0\">https://github.com/NuGet/NuGetGallery/compare/345ea25491...90a05bc3e0</a> continues on", result);
            }

            [Fact]
            public void UrlWithCallbacks()
            {
                //arrange
                var message = @"http://a.co/a.png#&quot;onerror=&#39;alert(&quot;Eek!&quot;)'";
                HashSet<string> extractedUrls;

                //act
                var result = TextTransform.TransformAndExtractUrls(message, out extractedUrls);

                //assert
                Assert.Equal(@"http://a.co/a.png#&quot;onerror=&#39;alert(&quot;Eek!&quot;)'", result);
            }

            [Fact]
            public void UrlWithAmpersand()
            {
                //arrange
                var message = "message http://google.com/?1&amp;2 continues on";
                HashSet<string> extractedUrls;

                //act
                var result = TextTransform.TransformAndExtractUrls(message, out extractedUrls);

                //assert
                Assert.Equal("message <a rel=\"nofollow external\" target=\"_blank\" href=\"http://google.com/?1&amp;2\" title=\"http://google.com/?1&amp;2\">http://google.com/?1&amp;2</a> continues on", result);
            }

            [Fact(Skip = "Encoding issues need to be resolved")]
            public void UrlWithInvalidButEscapedCharactersMatchesValidUrlSection()
            {
                //arrange
                var message = "message http://google.com/&lt;a&gt; continues on";
                HashSet<string> extractedUrls;

                //act
                var result = TextTransform.TransformAndExtractUrls(message, out extractedUrls);

                //assert
                Assert.Equal("message <a rel=\"nofollow external\" target=\"_blank\" href=\"http://google.com/\" title=\"http://google.com/\">http://google.com/</a><a> continues on", result);
            }

            [Fact(Skip = "Encoding issues need to be resolved")]
            public void UrlWithTrailingQuotationsMatchesUrlButNotTrailingQuotation()
            {
                // Arrange
                var message = "\"Check out www.Jabbr.net/\"";
                HashSet<string> extractedUrls;

                // Act
                var result = TextTransform.TransformAndExtractUrls(message, out extractedUrls);

                // Assert
                Assert.Equal("\"Check out <a rel=\"nofollow external\" target=\"_blank\" href=\"http://www.Jabbr.net/\" title=\"www.Jabbr.net/\">www.Jabbr.net/</a>\"", result);
            }

            [Fact(Skip = "Encoding issues need to be resolved")]
            public void EncodedUrlWithTrailingQuotationsMatchesUrlButNotTrailingQuotation()
            {
                // Arrange
                var message = "&quot;Visit http://www.jabbr.net/&quot;";
                HashSet<string> extractedUrls;

                // Act
                var result = TextTransform.TransformAndExtractUrls(message, out extractedUrls);

                // Assert
                Assert.Equal("\"Visit <a rel=\"nofollow external\" target=\"_blank\" href=\"http://www.jabbr.net/\" title=\"http://www.jabbr.net/\">http://www.jabbr.net/</a>\"", result);
            }

            [Fact]
            public void LocalHost()
            {
                //arrange
                var message = @"http://localhost/foo";
                HashSet<string> extractedUrls;

                //act
                var result = TextTransform.TransformAndExtractUrls(message, out extractedUrls);

                //assert
                Assert.Equal("<a rel=\"nofollow external\" target=\"_blank\" href=\"http://localhost/foo\" title=\"http://localhost/foo\">http://localhost/foo</a>", result);
            }

            [Fact]
            public void UrlsFollowedByACommaDontEncodeTheComma()
            {
                // Arrange
                var message = @"found him, hes https://twitter.com/dreamer3, sent him a tweet";
                HashSet<string> extractedUrls;

                // Act
                var result = TextTransform.TransformAndExtractUrls(message, out extractedUrls);

                // Assert
                Assert.Equal("found him, hes <a rel=\"nofollow external\" target=\"_blank\" href=\"https://twitter.com/dreamer3\" title=\"https://twitter.com/dreamer3\">https://twitter.com/dreamer3</a>, sent him a tweet", result);
            }

            [Fact]
            public void UrlsThatContainCommasAreEncodedEntirely()
            {
                // Arrange
                var message = @"found him, hes https://twitter.com/d,r,e,a,m,e,r,3, sent him a tweet";
                HashSet<string> extractedUrls;

                // Act
                var result = TextTransform.TransformAndExtractUrls(message, out extractedUrls);

                // Assert
                Assert.Equal("found him, hes <a rel=\"nofollow external\" target=\"_blank\" href=\"https://twitter.com/d,r,e,a,m,e,r,3\" title=\"https://twitter.com/d,r,e,a,m,e,r,3\">https://twitter.com/d,r,e,a,m,e,r,3</a>, sent him a tweet", result);
            }

            [Fact]
            public void LeftParenthesis()
            {
                //arrange
                var message = @"(http://foo.com";
                HashSet<string> extractedUrls;

                //act
                var result = TextTransform.TransformAndExtractUrls(message, out extractedUrls);

                //assert
                Assert.Equal("(<a rel=\"nofollow external\" target=\"_blank\" href=\"http://foo.com\" title=\"http://foo.com\">http://foo.com</a>", result);
            }

            [Fact]
            public void RightParenthesis()
            {
                //arrange
                var message = @"http://foo.com)";
                HashSet<string> extractedUrls;

                //act
                var result = TextTransform.TransformAndExtractUrls(message, out extractedUrls);

                //assert
                Assert.Equal("<a rel=\"nofollow external\" target=\"_blank\" href=\"http://foo.com\" title=\"http://foo.com\">http://foo.com</a>)", result);
            }

            [Fact]
            public void BothParenthesis()
            {
                //arrange
                var message = @"(http://foo.com)";
                HashSet<string> extractedUrls;

                //act
                var result = TextTransform.TransformAndExtractUrls(message, out extractedUrls);

                //assert
                Assert.Equal("(<a rel=\"nofollow external\" target=\"_blank\" href=\"http://foo.com\" title=\"http://foo.com\">http://foo.com</a>)", result);
            }

            [Fact]
            public void MSDN()
            {
                //arrange
                var message = @"http://msdn.microsoft.com/en-us/library/system.linq.enumerable(v=vs.110).aspx";
                HashSet<string> extractedUrls;

                //act
                var result = TextTransform.TransformAndExtractUrls(message, out extractedUrls);

                //assert
                Assert.Equal("<a rel=\"nofollow external\" target=\"_blank\" href=\"http://msdn.microsoft.com/en-us/library/system.linq.enumerable(v=vs.110).aspx\" title=\"http://msdn.microsoft.com/en-us/library/system.linq.enumerable(v=vs.110).aspx\">http://msdn.microsoft.com/en-us/library/system.linq.enumerable(v=vs.110).aspx</a>", result);
            }

            [Fact]
            public void MoreThanOneSetOfParens()
            {
                //arrange
                var message = @"http://foo.com/more_(than)_one_(parens)";
                HashSet<string> extractedUrls;

                //act
                var result = TextTransform.TransformAndExtractUrls(message, out extractedUrls);

                //assert
                Assert.Equal("<a rel=\"nofollow external\" target=\"_blank\" href=\"http://foo.com/more_(than)_one_(parens)\" title=\"http://foo.com/more_(than)_one_(parens)\">http://foo.com/more_(than)_one_(parens)</a>", result);
            }

            [Fact]
            public void WikiWithParensAndHash()
            {
                //arrange
                var message = @"http://foo.com/blah_(wikipedia)#cite-1";
                HashSet<string> extractedUrls;

                //act
                var result = TextTransform.TransformAndExtractUrls(message, out extractedUrls);

                //assert
                Assert.Equal("<a rel=\"nofollow external\" target=\"_blank\" href=\"http://foo.com/blah_(wikipedia)#cite-1\" title=\"http://foo.com/blah_(wikipedia)#cite-1\">http://foo.com/blah_(wikipedia)#cite-1</a>", result);
            }

            [Fact]
            public void WikiWithParensAndMoreAndHash()
            {
                //arrange
                var message = @"http://foo.com/blah_(wikipedia)_blah#cite-1";
                HashSet<string> extractedUrls;

                //act
                var result = TextTransform.TransformAndExtractUrls(message, out extractedUrls);

                //assert
                Assert.Equal("<a rel=\"nofollow external\" target=\"_blank\" href=\"http://foo.com/blah_(wikipedia)_blah#cite-1\" title=\"http://foo.com/blah_(wikipedia)_blah#cite-1\">http://foo.com/blah_(wikipedia)_blah#cite-1</a>", result);
            }

            [Fact]
            public void BitLyWithoutHttp()
            {
                //arrange
                var message = @"bit.ly/foo";
                HashSet<string> extractedUrls;

                //act
                var result = TextTransform.TransformAndExtractUrls(message, out extractedUrls);

                //assert
                Assert.Equal("<a rel=\"nofollow external\" target=\"_blank\" href=\"http://bit.ly/foo\" title=\"bit.ly/foo\">bit.ly/foo</a>", result);
            }

            [Fact]
            public void UnicodeInParens()
            {
                //arrange
                var message = @"http://foo.com/unicode_(✪)_in_parens";
                HashSet<string> extractedUrls;

                //act
                var result = TextTransform.TransformAndExtractUrls(message, out extractedUrls);

                //assert
                Assert.Equal("<a rel=\"nofollow external\" target=\"_blank\" href=\"http://foo.com/unicode_(&#10026;)_in_parens\" title=\"http://foo.com/unicode_(✪)_in_parens\">http://foo.com/unicode_(✪)_in_parens</a>", result);
            }

            [Fact]
            public void SomethingAfterParens()
            {
                //arrange
                var message = @"http://foo.com/(something)?after=parens";
                HashSet<string> extractedUrls;

                //act
                var result = TextTransform.TransformAndExtractUrls(message, out extractedUrls);

                //assert
                Assert.Equal("<a rel=\"nofollow external\" target=\"_blank\" href=\"http://foo.com/(something)?after=parens\" title=\"http://foo.com/(something)?after=parens\">http://foo.com/(something)?after=parens</a>", result);
            }

            [Fact]
            public void UrlInsideAQuotedSentence()
            {
                //arrange
                var message = "This is a sentence with quotes and a url ... see \"http://foo.com\"";
                HashSet<string> extractedUrls;

                //act
                var result = TextTransform.TransformAndExtractUrls(message, out extractedUrls);

                //assert
                Assert.Equal("This is a sentence with quotes and a url ... see \"<a rel=\"nofollow external\" target=\"_blank\" href=\"http://foo.com\" title=\"http://foo.com\">http://foo.com</a>\"", result);
            }

            [Fact]
            public void UrlEndsWithSlashInsideAQuotedSentence()
            {
                //arrange
                var message = "\"Visit http://www.jabbr.net/\"";
                HashSet<string> extractedUrls;

                //act
                var result = TextTransform.TransformAndExtractUrls(message, out extractedUrls);

                //assert
                Assert.Equal("\"Visit <a rel=\"nofollow external\" target=\"_blank\" href=\"http://www.jabbr.net/\" title=\"http://www.jabbr.net/\">http://www.jabbr.net/</a>\"", result);
            }

            [Fact]
            public void GoogleUrlWithQueryStringParams()
            {
                //arrange
                var message = "https://www.google.com/search?q=test+search&amp;sugexp=chrome,mod=14&amp;sourceid=chrome&amp;ie=UTF-8";
                HashSet<string> extractedUrls;

                //act
                var result = TextTransform.TransformAndExtractUrls(message, out extractedUrls);

                //assert
                Assert.Equal("<a rel=\"nofollow external\" target=\"_blank\" href=\"https://www.google.com/search?q=test+search&amp;sugexp=chrome,mod=14&amp;sourceid=chrome&amp;ie=UTF-8\" title=\"https://www.google.com/search?q=test+search&amp;sugexp=chrome,mod=14&amp;sourceid=chrome&amp;ie=UTF-8\">https://www.google.com/search?q=test+search&amp;sugexp=chrome,mod=14&amp;sourceid=chrome&amp;ie=UTF-8</a>", result);
                //Assert.Equal("<a rel=\"nofollow external\" target=\"_blank\" href=\"https://www.google.com/search?q=test+search&sugexp=chrome,mod=14&sourceid=chrome&ie=UTF-8\" title=\"https://www.google.com/search?q=test+search&amp;sugexp=chrome,mod=14&amp;sourceid=chrome&amp;ie=UTF-8\">https://www.google.com/search?q=test+search&amp;sugexp=chrome,mod=14&amp;sourceid=chrome&amp;ie=UTF-8</a>", result);
            }

            [Fact]
            public void DoNotUnescapeHtmlEntities()
            {
                //arrange
                var message = System.Web.HttpUtility.HtmlEncode("<a href=\"#\" onclick=\"alert('fail')>clickme</a>");
                HashSet<string> extractedUrls;

                //act
                var result = TextTransform.TransformAndExtractUrls(message, out extractedUrls);

                //assert
                Assert.Equal(message, result);
            }
        }
    }
}