using System;
using JabbR.Services;
using Lucene.Net.Store;
using Lucene.Net.Store.Azure;
using Microsoft.WindowsAzure.Storage;

namespace JabbR.Infrastructure
{
    public class LuceneAzureBlobFileSystem : ILuceneFileSystem
    {
        private readonly IApplicationSettings _settings;
        private const string JabbRSearchContainer = "jabbr-search";

        public LuceneAzureBlobFileSystem(IApplicationSettings settings)
        {
            _settings = settings;
        }

        private AzureDirectory _indexDir;
        public Directory IndexDirectory
        {
            get
            {
                if (_indexDir == null)
                {
                    var account = CloudStorageAccount.Parse(_settings.AzureblobStorageConnectionString);
                    _indexDir = new AzureDirectory(account, JabbRSearchContainer, new RAMDirectory());
                }

                return _indexDir;
            }
        }

        public ILuceneIndexMetaData MetaData { get; private set; }

        private class LuceneAzureBlogFileSystemMetaData : ILuceneIndexMetaData
        {
            public int? LastMessageKey { get; private set; }
            public DateTime? LastWriteTime { get; private set; }
            public DateTime? CreationTime { get; private set; }

            public bool RequiresRefresh()
            {
                throw new NotImplementedException();
            }

            public void UpdateLastWriteTime(DateTime lastWriteTime, int lastMessageKey)
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