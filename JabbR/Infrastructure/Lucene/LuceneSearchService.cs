using System;
using System.Collections.Generic;
using System.Linq;
using JabbR.Models;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.QueryParsers;
using Lucene.Net.Search;

namespace JabbR.Infrastructure
{
    public class LuceneSearchService : ISearchService
    {
        private readonly Lucene.Net.Store.Directory _directory;

        private static readonly Dictionary<string, float> SearchFields = new Dictionary<string, float> 
        {
            { "Content", 1.5f },
        };

        public LuceneSearchService(Lucene.Net.Store.Directory directory)
        {
            _directory = directory;
        }

        public IList<ChatMessage> Search(string text, int skip, int take, out int totalHits)
        {
            int numRecords = skip + take;

            var searcher = new IndexSearcher(_directory, readOnly: true);
            var query = MoreComplexQuery(QueryParser.Escape(text));

            var results = searcher.Search(query, n: numRecords, sort: new Sort(SortField.FIELD_SCORE), filter: null);
            totalHits = results.TotalHits;

            if (totalHits == 0)
            {
                return new List<ChatMessage>(0);
            }

            var chatMessages = results.ScoreDocs
                                  .Skip(skip)
                                  .Select(sd => GetChatMessage(searcher.Doc(sd.Doc)))
                                  .ToList();

            return chatMessages;
        }

        private ChatMessage GetChatMessage(Document doc)
        {
            return new ChatMessage()
            {
                Content = doc.GetField("Content").StringValue,
            };
        }

        private static Query MoreComplexQuery(string searchTerm)
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