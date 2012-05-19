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
    public class QueryStringCollectionTests
    {
        public class Constructor
        {
            private Uri MakeUri(string parameters)
            {
                return new Uri("http://example.com?" + parameters);
            }

            [Fact]
            public void ShouldReturnFirstAndSecondParameter()
            {
                var uri = MakeUri("first=value1&second=value2");
                var collection = new QueryStringCollection(uri);

                Assert.Equal("value1", collection["first"]);
                Assert.Equal("value2", collection["second"]);
            }
            [Fact]
            public void ShouldHandleNameOnlyParameters()
            {
                var uri = MakeUri("first=value1&second");
                var collection = new QueryStringCollection(uri);

                Assert.Equal(null, collection["second"]);
            }
            [Fact]
            public void ShouldHandleMultipleAmpersands()
            {
                var uri = MakeUri("first=value1&second&&");
                var collection = new QueryStringCollection(uri);

                Assert.Equal(2, collection.Count);
            }
            [Fact]
            public void ShouldThrowInvalidArgumentExceptionWhenUriIsNull()
            {
                Assert.Throws<ArgumentNullException>(() =>
                {
                    var collection = new QueryStringCollection(null);
                });
            }

        }
        public class TryGetAndConvert
        {
            QueryStringCollection _QueryStringCollection;
            public TryGetAndConvert()
            {
                _QueryStringCollection = new QueryStringCollection(new Uri("http://example.com?intvalue=1&stringvalue=str&boolvalue=true&empty"));
            }
            [Fact]
            public void ShouldOutputValueForString()
            {
                string output;
                var result = _QueryStringCollection.TryGetAndConvert<string>("stringvalue", out output);

                Assert.True(result);
                Assert.Equal("str", output); 
                
            }

            [Fact]
            public void ShouldOutputValueForInt()
            {
                int output;
                var result = _QueryStringCollection.TryGetAndConvert<int>("intvalue", out output);

                Assert.True(result);
                Assert.Equal(1, output);
            }

            [Fact]
            public void ShouldOutputValueForBool()
            {
                bool output;
                var result = _QueryStringCollection.TryGetAndConvert<bool>("boolvalue", out output);

                Assert.True(result);
                Assert.Equal(true, output);
            }

            [Fact]
            public void ShouldOutputNullForNonexistantString()
            {
                string output;
                var result = _QueryStringCollection.TryGetAndConvert<string>("stringvalue1", out output);

                Assert.True(result);
                Assert.Equal(null, output);
            }

            [Fact]
            public void ShouldOutputZeroForNonexistantInt()
            {
                int output;
                var result = _QueryStringCollection.TryGetAndConvert<int>("nonexistant", out output);

                Assert.True(result);
                Assert.Equal(0, output);
            }
            [Fact]
            public void ShouldReturnFalseForEmptyValueType()
            {
                int output;
                var result = _QueryStringCollection.TryGetAndConvert<int>("empty", out output);

                Assert.False(result);
            }
            [Fact]
            public void ShouldReturnTrueForEmptyReferenceType()
            {
                string output;
                var result = _QueryStringCollection.TryGetAndConvert<string>("empty", out output);

                Assert.True(result);
            }
            [Fact]
            public void ShouldReturnTrueForEmptyNullableType()
            {
                bool? output;
                var result = _QueryStringCollection.TryGetAndConvert<bool?>("empty", out output);

                Assert.True(result);
            }
        }
        public class Indexer
        {
            QueryStringCollection _QueryStringCollection;
            public Indexer()
            {
                _QueryStringCollection = new QueryStringCollection(new Uri("http://example.com?intvalue=1&stringvalue=str&boolvalue=true&empty"));
            }

            [Fact]
            public void ShouldReturnNullForNonExistantEntry()
            {
                Assert.Null(_QueryStringCollection["nonexistant"]);
            }
        }
    }
}
