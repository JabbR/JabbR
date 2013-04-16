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
            DateTime? lastWriteTime = GetLastWriteTime();

            if ((lastWriteTime == null) || IndexRequiresRefresh() || forceRefresh)
            {
                EnsureIndexWriter(creatingIndex: true);
                _indexWriter.DeleteAll();
                _indexWriter.Commit();

                // Reset the lastWriteTime to null. This will allow us to get a fresh copy of all the latest \ latest successful chatMessages
                lastWriteTime = null;

                // Set the index create time to now. This would tell us when we last rebuilt the index.
                UpdateIndexRefreshTime();
            }

            var count = GetChatMessagesCountSinceLastIndex(lastWriteTime);
            if (count > 0)
            {
                EnsureIndexWriter(creatingIndex: lastWriteTime == null);
                AddChatMessages(lastWriteTime, count);
            }

            UpdateLastWriteTime();
        }

        private int GetChatMessagesCountSinceLastIndex(DateTime? lastIndexTime)
        {
            using (var repo = _repositoryFunc())
            {
                return repo.GetMessageCountSince(lastIndexTime);
            }
        }

        private void AddChatMessages(DateTime? lastWriteTime, int totalCount)
        {
            const int perPage = 100;
            int totalPages = (int)Math.Ceiling((double)totalCount / perPage);

            // As per http://stackoverflow.com/a/3894582. The IndexWriter is CPU bound, so we can try and write multiple chatMessages in parallel.
            // The IndexWriter is thread safe and is primarily CPU-bound.

            Parallel.For(0, totalPages, i =>
            {
                using (var repo = _repositoryFunc())
                {
                    var messages = repo.GetMessagesToIndex(lastWriteTime, i * perPage, perPage).ToList();
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
            var got = WriterCache.GetOrAdd(_fileSystem, _indexWriter);
        }

        protected internal bool IndexRequiresRefresh()
        {
            return _fileSystem.MetaData.RequiresRefresh();
        }

        protected internal DateTime? GetLastWriteTime()
        {
            return _fileSystem.MetaData.LastWriteTime;
        }

        protected internal void UpdateLastWriteTime()
        {
            _fileSystem.MetaData.UpdateLastWriteTime(DateTime.UtcNow);
        }

        protected internal void UpdateIndexRefreshTime()
        {
            _fileSystem.MetaData.UpdateCreationTime(DateTime.UtcNow);
        }
    }
}