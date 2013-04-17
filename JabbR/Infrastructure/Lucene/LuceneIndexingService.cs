using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using JabbR.Models;
using JabbR.Services;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Documents;
using Lucene.Net.Index;

namespace JabbR.Infrastructure
{
    public class LuceneIndexingService : ISearchIndexingService
    {
        private static readonly object IndexWriterLock = new object();

        private static readonly ConcurrentDictionary<ILuceneFileSystem, IndexWriter> WriterCache =
            new ConcurrentDictionary<ILuceneFileSystem, IndexWriter>();

        private readonly ILuceneFileSystem _fileSystem;

        private readonly Func<IJabbrRepository> _repositoryFunc;

        private IndexWriter _indexWriter;

        public LuceneIndexingService(Func<IJabbrRepository> repositoryFunc, ILuceneFileSystem fileSystem)
        {
            _repositoryFunc = repositoryFunc;
            _fileSystem = fileSystem;
        }

        public void UpdateIndex()
        {
            UpdateIndex(forceRefresh: false);
        }

        internal void UpdateIndex(bool forceRefresh)
        {
            int? lastMessageKey = _fileSystem.MetaData.LastMessageKey;

            if (lastMessageKey == null || _fileSystem.MetaData.RequiresRefresh() || forceRefresh)
            {
                EnsureIndexWriter(creatingIndex: true);
                _indexWriter.DeleteAll();
                _indexWriter.Commit();

                // Set the index create time to now. This would tell us when we last rebuilt the index.
                UpdateIndexRefreshTime();
            }

            IndexTaskMetaData indexTask = GetIndexTaskMetaData(lastMessageKey);
            if (indexTask.TotalItemsToIndex > 0)
            {
                EnsureIndexWriter(creatingIndex: lastMessageKey == null);
                AddChatMessages(indexTask);
            }

            UpdateIndexMetaData(indexTask);
        }

        private IndexTaskMetaData GetIndexTaskMetaData(int? lastMessageKey)
        {
            using (var repo = _repositoryFunc())
            {
                int newestMessageKey;
                int messageCountToIndex = repo.GetMessageCountSince(lastMessageKey, out newestMessageKey);
                
                return new IndexTaskMetaData()
                {
                    LowerBoundMessageKey = lastMessageKey,
                    UpperBoundMessageKey = newestMessageKey,
                    TotalItemsToIndex = messageCountToIndex,
                };
            }
        }

        private void AddChatMessages(IndexTaskMetaData indexTaskMetaData)
        {
            const int perPage = 100;
            int totalPages = (int)Math.Ceiling((double)indexTaskMetaData.TotalItemsToIndex / perPage);

            // As per http://stackoverflow.com/a/3894582. The IndexWriter is CPU bound, so we can try and write multiple chatMessages in parallel.
            // The IndexWriter is thread safe and is primarily CPU-bound.

            Parallel.For(0, totalPages, i =>
            {
                using (var repo = _repositoryFunc())
                {
                    var messages = repo.GetMessagesToIndex(indexTaskMetaData.LowerBoundMessageKey, indexTaskMetaData.UpperBoundMessageKey, i * perPage, perPage).ToList();
                    foreach (var chatMessage in messages)
                    {
                        AddChatMessage(chatMessage);
                    }
                }
            });

            _indexWriter.Commit();
        }


        private void AddChatMessage(ChatMessage chatMessage)
        {
            var document = new Document();

            var field = new Field("Id", chatMessage.Id.ToStringSafe().ToLowerInvariant(), Field.Store.YES, Field.Index.NOT_ANALYZED);
            document.Add(field);

            field = new Field("Content", chatMessage.Content.ToStringSafe(), Field.Store.YES, Field.Index.ANALYZED);
            document.Add(field);

            field = new Field("RoomName", chatMessage.Room.Name.ToStringSafe(), Field.Store.YES, Field.Index.ANALYZED);
            field.Boost = 0.1f;
            document.Add(field);

            field = new Field("UserName", chatMessage.User.Name.ToStringSafe(), Field.Store.YES, Field.Index.ANALYZED);
            document.Add(field);

            field = new Field("UserId", chatMessage.User.Name.ToStringSafe(), Field.Store.YES, Field.Index.NOT_ANALYZED);
            document.Add(field);

            field = new Field("HtmlContent", chatMessage.HtmlContent.ToStringSafe(), Field.Store.YES, Field.Index.NOT_ANALYZED);
            document.Add(field);

            field = new Field("HtmlEncoded", chatMessage.HtmlEncoded.ToStringSafe().ToLowerInvariant(), Field.Store.YES, Field.Index.NOT_ANALYZED);
            document.Add(field);

            field = new Field("MessageType", chatMessage.MessageType.ToStringSafe(), Field.Store.YES, Field.Index.NOT_ANALYZED);
            document.Add(field);

            field = new Field("ImageUrl", chatMessage.ImageUrl.ToStringSafe(), Field.Store.YES, Field.Index.NOT_ANALYZED);
            document.Add(field);

            field = new Field("When", chatMessage.When.ToString("s"), Field.Store.YES, Field.Index.ANALYZED);
            document.Add(field);

            field = new Field("Source", chatMessage.Source.ToStringSafe(), Field.Store.YES, Field.Index.ANALYZED);
            field.Boost = 0.025f;
            document.Add(field);

            _indexWriter.AddDocument(document);
        }

        protected void EnsureIndexWriter(bool creatingIndex)
        {
            if (_indexWriter == null)
            {
                if (WriterCache.TryGetValue(_fileSystem, out _indexWriter))
                {
                    return;
                }

                lock (IndexWriterLock)
                {
                    if (WriterCache.TryGetValue(_fileSystem, out _indexWriter))
                    {
                        return;
                    }

                    EnsureIndexWriterCore(creatingIndex);
                }
            }
        }

        private void EnsureIndexWriterCore(bool creatingIndex)
        {
            var analyzer = new StandardAnalyzer(LuceneCommon.LuceneVersion);
            _indexWriter = new IndexWriter(_fileSystem.IndexDirectory, analyzer, create: creatingIndex, mfl: IndexWriter.MaxFieldLength.UNLIMITED);

            // Should always be add, due to locking
            WriterCache.GetOrAdd(_fileSystem, _indexWriter);
        }

        private void UpdateIndexMetaData(IndexTaskMetaData indexTaskMetaData)
        {
            _fileSystem.MetaData.UpdateLastWriteTime(DateTime.UtcNow, indexTaskMetaData.UpperBoundMessageKey);
        }

        private void UpdateIndexRefreshTime()
        {
            _fileSystem.MetaData.UpdateCreationTime(DateTime.UtcNow);
        }

        private class IndexTaskMetaData
        {
            public int? LowerBoundMessageKey { get; set; }
            public int UpperBoundMessageKey { get; set; }
            public int TotalItemsToIndex { get; set; }
        }
    }
}