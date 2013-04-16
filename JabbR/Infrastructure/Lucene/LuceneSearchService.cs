using System;
using System.Collections.Generic;
using System.Linq;
using JabbR.ViewModels;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.QueryParsers;
using Lucene.Net.Search;
using Lucene.Net.Store;
using PagedList;

namespace JabbR.Infrastructure
{
    public class LuceneSearchService : ISearchService
    {
        private readonly ILuceneFileSystem _fileSystem;

        private static readonly Dictionary<string, float> SearchFields = new Dictionary<string, float> 
        {
            { "Content", 1.5f },
            { "RoomName", 0.1f },
            { "UserName", 0.025f },
        };

        public LuceneSearchService(ILuceneFileSystem fileSystem)
        {
            _fileSystem = fileSystem;
        }

        public IPagedList<SearchMessageViewModel> Search(SearchRequest request)
        {
            int skip = (request.CurrentPage-1) * request.PerPage;
            int numRecords = skip + request.PerPage;

            var searcher = new IndexSearcher(_fileSystem.IndexDirectory, readOnly: true);
            var query = BuildSearchQuery(request.SearchQuery);

            var results = searcher.Search(query, n: numRecords, sort: new Sort(SortField.FIELD_SCORE), filter: null);
            int totalHits = results.TotalHits;

            if (totalHits == 0)
            {
                return new StaticPagedList<SearchMessageViewModel>(Enumerable.Empty<SearchMessageViewModel>(), request.CurrentPage, request.PerPage, 0);
            }

            var chatMessages = results.ScoreDocs
                                  .Skip(skip)
                                  .Select(sd => GetChatMessage(searcher.Doc(sd.Doc)))
                                  .ToList();

            return new StaticPagedList<SearchMessageViewModel>(chatMessages, request.CurrentPage, request.PerPage, totalHits);
        }

        private SearchMessageViewModel GetChatMessage(Document doc)
        {
            return new SearchMessageViewModel()
            {
                Content = doc.GetField("Content").StringValue,
                Id = doc.GetField("Id").StringValue,

                RoomName = doc.GetField("RoomName").StringValue,

                UserName = doc.GetField("UserName").StringValue,
                UserId = doc.GetField("UserId").StringValue,

                HtmlContent = doc.GetField("HtmlContent").StringValue,
                HtmlEncoded = Boolean.Parse(doc.GetField("HtmlEncoded").StringValue),

                MessageType = Int32.Parse(doc.GetField("MessageType").StringValue),

                ImageUrl = doc.GetField("ImageUrl").StringValue,
                When = DateTimeOffset.Parse(doc.GetField("When").StringValue),
                Source = doc.GetField("Source").StringValue,
            };
        }

        private static Query BuildSearchQuery(string searchTerm)
        {
            var analyzer = new StandardAnalyzer(LuceneCommon.LuceneVersion);
            searchTerm = QueryParser.Escape(searchTerm).ToLowerInvariant();

            var queryParser = new MultiFieldQueryParser(LuceneCommon.LuceneVersion, SearchFields.Keys.ToArray(), analyzer, SearchFields);

            var conjuctionQuery = new BooleanQuery();
            conjuctionQuery.Boost = 1.5f;

            var exactIdQuery = new TermQuery(new Term("Id", searchTerm));
            exactIdQuery.Boost = 10.0f;

            var wildCardQuery = new BooleanQuery();

            foreach (var term in searchTerm.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries))
            {
                if (!StandardAnalyzer.STOP_WORDS_SET.Contains(term))
                {
                    foreach (var field in SearchFields)
                    {
                        conjuctionQuery.Add(queryParser.Parse(term), Occur.MUST);

                        var wildCardTermQuery = new WildcardQuery(new Term(field.Key, term + "*"));
                        wildCardTermQuery.Boost = 0.7f * field.Value;

                        wildCardQuery.Add(wildCardTermQuery, Occur.SHOULD);
                    }
                }
            }

            return conjuctionQuery.Combine(new Query[] { exactIdQuery, conjuctionQuery, wildCardQuery });
        }
    }
}