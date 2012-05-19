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
            [Fact]
            public void ShouldReturnFirstAndSecondParameter()
            {
                var collection = new QueryStringCollection(
                    new Uri("http://example.com?first=value1&second=value2")
                    );

                Assert.Equal("value1", collection["first"]);
                Assert.Equal("value2", collection["second"]);
            }
            [Fact]
            public void ShouldHandleNameOnlyParameters()
            {
                var collection = new QueryStringCollection(
                    new Uri("http://example.com?first=value1&second")
                    );

                Assert.Equal(null, collection["second"]);
            }
            [Fact]
            public void ShouldHandleMultipleAmpersands()
            {
                var collection = new QueryStringCollection(
                    new Uri("http://example.com?first=value1&second=value2&&")
                    );

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
            [Fact]
            public void ShouldOutputValueForString()
            {
                var _QueryStringCollection = new QueryStringCollection(
                    new Uri("http://example.com?intvalue=1&stringvalue=str&boolvalue=true&empty")
                    );

                string output;
                var result = _QueryStringCollection.TryGetAndConvert<string>("stringvalue", out output);

                Assert.True(result);
                Assert.Equal("str", output); 
                
            }

            [Fact]
            public void ShouldOutputValueForInt()
            {
                var _QueryStringCollection = new QueryStringCollection(
                    new Uri("http://example.com?intvalue=1&stringvalue=str&boolvalue=true&empty")
                    );

                int output;
                var result = _QueryStringCollection.TryGetAndConvert<int>("intvalue", out output);

                Assert.True(result);
                Assert.Equal(1, output);
            }

            [Fact]
            public void ShouldOutputValueForBool()
            {
                var _QueryStringCollection = new QueryStringCollection(
                    new Uri("http://example.com?intvalue=1&stringvalue=str&boolvalue=true&empty")
                    );

                bool output;
                var result = _QueryStringCollection.TryGetAndConvert<bool>("boolvalue", out output);

                Assert.True(result);
                Assert.Equal(true, output);
            }

            [Fact]
            public void ShouldOutputNullForNonexistantString()
            {
                var _QueryStringCollection = new QueryStringCollection(
                    new Uri("http://example.com?intvalue=1&stringvalue=str&boolvalue=true&empty")
                    );

                string output;
                var result = _QueryStringCollection.TryGetAndConvert<string>("stringvalue1", out output);

                Assert.True(result);
                Assert.Equal(null, output);
            }

            [Fact]
            public void ShouldOutputZeroForNonexistantInt()
            {
                var _QueryStringCollection = new QueryStringCollection(
                    new Uri("http://example.com?intvalue=1&stringvalue=str&boolvalue=true&empty")
                    );

                int output;
                var result = _QueryStringCollection.TryGetAndConvert<int>("nonexistant", out output);

                Assert.True(result);
                Assert.Equal(0, output);
            }
            [Fact]
            public void ShouldReturnFalseForEmptyValueType()
            {
                var _QueryStringCollection = new QueryStringCollection(
                    new Uri("http://example.com?intvalue=1&stringvalue=str&boolvalue=true&empty")
                    );

                int output;
                var result = _QueryStringCollection.TryGetAndConvert<int>("empty", out output);

                Assert.False(result);
            }
            [Fact]
            public void ShouldReturnTrueForEmptyReferenceType()
            {
                var _QueryStringCollection = new QueryStringCollection(
                    new Uri("http://example.com?intvalue=1&stringvalue=str&boolvalue=true&empty")
                    );

                string output;
                var result = _QueryStringCollection.TryGetAndConvert<string>("empty", out output);

                Assert.True(result);
            }
            [Fact]
            public void ShouldReturnTrueForEmptyNullableType()
            {
                var _QueryStringCollection = new QueryStringCollection(
                    new Uri("http://example.com?intvalue=1&stringvalue=str&boolvalue=true&empty")
                    );

                bool? output;
                var result = _QueryStringCollection.TryGetAndConvert<bool?>("empty", out output);

                Assert.True(result);
            }
        }
        public class Indexer
        {
            [Fact]
            public void ShouldReturnNullForNonExistantEntry()
            {
                var _QueryStringCollection = new QueryStringCollection(
                    new Uri("http://example.com?intvalue=1&stringvalue=str&boolvalue=true&empty")
                    );

                Assert.Null(_QueryStringCollection["nonexistant"]);
            }
        }
    }
}
