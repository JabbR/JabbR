using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;
using Moq;
using System.Web.Http.Routing;
using System.Net.Http;
using JabbR.Infrastructure;

namespace JabbR.Tests
{
    public class UriExtensionsTests
    {
        public class Value
        {
            IDictionary<string, string> _Dictionary;
            public Value()
            {
                _Dictionary = new Dictionary<string, string> { 
                    {"intvalue", "1"},
                    {"stringvalue", "str"},
                    {"boolvalue", "true"},
                    {"empty", null}
                };
            }
            [Fact]
            public void ShouldOutputValueForString()
            {
                string output;
                var result = _Dictionary.TryGetAndConvert<string>("stringvalue", out output);

                Assert.True(result);
                Assert.Equal("str", output);
                
            }

            [Fact]
            public void ShouldOutputValueForInt()
            {
                int output;
                var result = _Dictionary.TryGetAndConvert<int>("intvalue", out output);

                Assert.True(result);
                Assert.Equal(1, output);
            }

            [Fact]
            public void ShouldOutputValueForBool()
            {
                bool output;
                var result = _Dictionary.TryGetAndConvert<bool>("boolvalue", out output);

                Assert.True(result);
                Assert.Equal(true, output);
            }

            [Fact]
            public void ShouldOutputNullForNonexistantString()
            {
                string output;
                var result = _Dictionary.TryGetAndConvert<string>("stringvalue1", out output);

                Assert.True(result);
                Assert.Equal(null, output);
            }

            [Fact]
            public void ShouldOutputZeroForNonexistantInt()
            {
                int output;
                var result = _Dictionary.TryGetAndConvert<int>("nonexistant", out output);

                Assert.True(result);
                Assert.Equal(0, output);
            }
            [Fact]
            public void ShouldReturnFalseForEmptyValueType()
            {
                int output;
                var result = _Dictionary.TryGetAndConvert<int>("empty", out output);

                Assert.False(result);
            }
            [Fact]
            public void ShouldReturnTrueForEmptyReferenceType()
            {
                string output;
                var result = _Dictionary.TryGetAndConvert<string>("empty", out output);

                Assert.True(result);
            }
            [Fact]
            public void ShouldReturnTrueForEmptyNullableType()
            {
                bool? output;
                var result = _Dictionary.TryGetAndConvert<bool?>("empty", out output);

                Assert.True(result);
            }
        }
        public class QueryString
        {
            private Uri MakeUri(string parameters)
            {
                return new Uri("http://example.com?" + parameters);
            }

            [Fact]
            public void ShouldReturnFirstAndSecondParameter()
            {
                var uri = MakeUri("first=value1&second=value2");
                var result = uri.QueryString();

                Assert.Equal("value1", result["first"]);
                Assert.Equal("value2", result["second"]);
            }
            [Fact]
            public void ShouldHandleNameOnlyParameters()
            {
                var uri = MakeUri("first=value1&second");
                var result = uri.QueryString();

                Assert.Equal(null, result["second"]);
            }
            [Fact]
            public void ShouldHandleMultipleAmpersands()
            {
                var uri = MakeUri("first=value1&second&&");
                var result = uri.QueryString();

                Assert.Equal(2, result.Count);
            }
            [Fact]
            public void ShouldThrowInvalidArgumentExceptionWhenUriIsNull()
            {
                Assert.Throws<ArgumentNullException>(() =>
                {
                    var result = ((Uri)null).QueryString();
                });
            }

        }
    }
}
