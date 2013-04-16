using System;
using System.IO;
using JabbR.Services;
using Lucene.Net.Store;
using Microsoft.WindowsAzure.Storage;
using Directory = System.IO.Directory;
using LuceneDirectory = Lucene.Net.Store.Directory;
using LuceneAzureDirectory = Lucene.Net.Store.Azure.AzureDirectory;

namespace JabbR.Infrastructure
{
    public interface ILuceneFileSystem
    {
        LuceneDirectory IndexDirectory { get; }
        ILuceneIndexMetaData MetaData { get; }
    }

    public interface ILuceneIndexMetaData
    {
        string LastMessageId { get; }
        DateTime? LastWriteTime { get; }
        DateTime? CreationTime { get; }

        bool RequiresRefresh();
        void UpdateLastWriteTime(DateTime lastWriteTime);
        void UpdateCreationTime(DateTime creationTime);
    }

    public class LuceneLocalFileSystem : ILuceneFileSystem
    {
        internal static readonly string IndexDirectoryPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"App_Data\\Lucene");
        internal static readonly string IndexMetadataPath = Path.Combine(IndexDirectoryPath ?? ".", "index.metadata");
        private static readonly TimeSpan IndexRecreateInterval = TimeSpan.FromDays(3);

        private SimpleFSDirectory _indexDir;
        public LuceneDirectory IndexDirectory
        {
            get
            {
                if (_indexDir == null)
                {
                    if (!Directory.Exists(IndexDirectoryPath))
                    {
                        Directory.CreateDirectory(IndexDirectoryPath);
                    }

                    var directoryInfo = new DirectoryInfo(IndexDirectoryPath);
                    _indexDir = new SimpleFSDirectory(directoryInfo);
                }

                return _indexDir;
            }
        }

        private LuceneLocalFileSystemMetaData _metaData;
        public ILuceneIndexMetaData MetaData
        {
            get
            {
                if (_metaData == null)
                {
                    _metaData = new LuceneLocalFileSystemMetaData();
                }

                return _metaData;
            }
        }

        private class LuceneLocalFileSystemMetaData : ILuceneIndexMetaData
        {
            public string LastMessageId { get; private set; }
            public DateTime? LastWriteTime
            {
                get
                {
                    if (!File.Exists(IndexMetadataPath))
                    {
                        return null;
                    }
                    return File.GetLastWriteTimeUtc(IndexMetadataPath);
                }
            }

            public DateTime? CreationTime
            {
                get
                {
                    if (File.Exists(IndexMetadataPath))
                    {
                        return File.GetCreationTimeUtc(IndexMetadataPath);
                    }

                    return null;
                }
            }

            public bool RequiresRefresh()
            {
                if (File.Exists(IndexMetadataPath))
                {
                    var creationTime = File.GetCreationTimeUtc(IndexMetadataPath);
                    return (DateTime.UtcNow - creationTime) > IndexRecreateInterval;
                }

                // If we've never created the index, it needs to be refreshed.
                return true;
            }

            public void UpdateLastWriteTime(DateTime lastWriteTime)
            {
                if (!File.Exists(IndexMetadataPath))
                {
                    // Create the index and add a timestamp to it that specifies the time at which it was created.
                    File.WriteAllBytes(IndexMetadataPath, new byte[0]);
                }
                else
                {
                    File.SetLastWriteTimeUtc(IndexMetadataPath, DateTime.UtcNow);
                }
            }

            public void UpdateCreationTime(DateTime creationTime)
            {
                if (File.Exists(IndexMetadataPath))
                {
                    File.SetCreationTimeUtc(IndexMetadataPath, DateTime.UtcNow);
                }
            }
        }
    }

    public class LuceneAzureBlobFileSystem : ILuceneFileSystem
    {
        private readonly IApplicationSettings _settings;
        private const string JabbRSearchContainer = "jabbr-search";

        public LuceneAzureBlobFileSystem(IApplicationSettings settings)
        {
            _settings = settings;
        }

        private LuceneAzureDirectory _indexDir;
        public LuceneDirectory IndexDirectory
        {
            get
            {
                if (_indexDir == null)
                {
                    var account = CloudStorageAccount.Parse(_settings.AzureblobStorageConnectionString);
                    _indexDir = new LuceneAzureDirectory(account, JabbRSearchContainer, new RAMDirectory());
                }

                return _indexDir;
            }
        }

        public ILuceneIndexMetaData MetaData { get; private set; }

        private class LuceneAzureBlogFileSystemMetaData : ILuceneIndexMetaData
        {
            public string LastMessageId { get; private set; }
            public DateTime? LastWriteTime { get; private set; }
            public DateTime? CreationTime { get; private set; }
            public bool RequiresRefresh()
            {
                throw new NotImplementedException();
            }

            public void UpdateLastWriteTime(DateTime lastWriteTime)
            {
                throw new NotImplementedException();
            }

            public void UpdateCreationTime(DateTime creationTime)
            {
                throw new NotImplementedException();
            }
        }
    }
}
