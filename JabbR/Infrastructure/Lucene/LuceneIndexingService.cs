using System;
using System.Collections.Concurrent;
using System.IO;
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

        private static readonly TimeSpan IndexRecreateInterval = TimeSpan.FromDays(3);

        private static readonly ConcurrentDictionary<Lucene.Net.Store.Directory, IndexWriter> WriterCache =
            new ConcurrentDictionary<Lucene.Net.Store.Directory, IndexWriter>();

        private readonly Lucene.Net.Store.Directory _directory;

        private readonly Func<IJabbrRepository> _repositoryFunc;

        private IndexWriter _indexWriter;

        public LuceneIndexingService(Func<IJabbrRepository> repositoryFunc, Lucene.Net.Store.Directory directory)
        {
            _repositoryFunc = repositoryFunc;
            _directory = directory;
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
                if (WriterCache.TryGetValue(_directory, out _indexWriter))
                {
                    return;
                }

                lock (IndexWriterLock)
                {
                    if (WriterCache.TryGetValue(_directory, out _indexWriter))
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
            _indexWriter = new IndexWriter(_directory, analyzer, create: creatingIndex, mfl: IndexWriter.MaxFieldLength.UNLIMITED);

            // Should always be add, due to locking
            var got = WriterCache.GetOrAdd(_directory, _indexWriter);
        }

        protected internal static bool IndexRequiresRefresh()
        {
            if (File.Exists(LuceneCommon.IndexMetadataPath))
            {
                var creationTime = File.GetCreationTimeUtc(LuceneCommon.IndexMetadataPath);
                return (DateTime.UtcNow - creationTime) > IndexRecreateInterval;
            }

            // If we've never created the index, it needs to be refreshed.
            return true;
        }

        protected internal virtual DateTime? GetLastWriteTime()
        {
            if (!File.Exists(LuceneCommon.IndexMetadataPath))
            {
                return null;
            }
            return File.GetLastWriteTimeUtc(LuceneCommon.IndexMetadataPath);
        }

        protected internal virtual void UpdateLastWriteTime()
        {
            if (!File.Exists(LuceneCommon.IndexMetadataPath))
            {
                // Create the index and add a timestamp to it that specifies the time at which it was created.
                File.WriteAllBytes(LuceneCommon.IndexMetadataPath, new byte[0]);
            }
            else
            {
                File.SetLastWriteTimeUtc(LuceneCommon.IndexMetadataPath, DateTime.UtcNow);
            }
        }

        protected static void UpdateIndexRefreshTime()
        {
            if (File.Exists(LuceneCommon.IndexMetadataPath))
            {
                File.SetCreationTimeUtc(LuceneCommon.IndexMetadataPath, DateTime.UtcNow);
            }
        }
    }
}