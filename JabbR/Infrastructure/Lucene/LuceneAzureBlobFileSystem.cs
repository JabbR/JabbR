using System;
using System.IO;
using JabbR.Services;
using Lucene.Net.Store;
using Lucene.Net.Store.Azure;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Newtonsoft.Json;
using Directory = Lucene.Net.Store.Directory;

namespace JabbR.Infrastructure
{
    public class LuceneAzureBlobFileSystem : ILuceneFileSystem
    {
        private const string JabbRSearchIndexContainer = "jabbr-search";
        private const string JabbRSearchMetaDataContainer = "jabbr-search-metadata";
        private readonly IApplicationSettings _settings;
        private readonly CloudStorageAccount _account;

        public LuceneAzureBlobFileSystem(IApplicationSettings settings)
        {
            _settings = settings;
            _account = CloudStorageAccount.Parse(_settings.AzureblobStorageConnectionString);
            MetaData = new LuceneAzureBlogFileSystemMetaData(_account);
        }

        private AzureDirectory _indexDir;
        public Directory IndexDirectory
        {
            get
            {
                if (_indexDir == null)
                {
                    _indexDir = new AzureDirectory(_account, JabbRSearchIndexContainer, new RAMDirectory());
                }

                return _indexDir;
            }
        }

        public ILuceneIndexMetaData MetaData { get; private set; }

        private class LuceneAzureBlogFileSystemMetaData : ILuceneIndexMetaData
        {
            private const string JabbRSearchMetaDataFile = "index.metadata";
            private readonly CloudStorageAccount _account;

            private AzureBlobStorageIndexMetaData _internalData;

            public LuceneAzureBlogFileSystemMetaData(CloudStorageAccount account)
            {
                _account = account;
                _internalData = new AzureBlobStorageIndexMetaData();
                ReloadInternalData();
            }

            public int? LastMessageKey { get { return _internalData.LastMessageKey; } }
            public DateTime? LastWriteTime { get { return _internalData.LastWriteTime; } }
            public DateTime? CreationTime { get { return _internalData.CreationTime; } }

            public bool RequiresRefresh()
            {
                return LastMessageKey == null;
            }

            public void UpdateLastWriteTime(DateTime lastWriteTime, int lastMessageKey)
            {
                _internalData.LastMessageKey = lastMessageKey;
                _internalData.LastWriteTime = lastWriteTime;

                SaveInternalData();
            }

            public void UpdateCreationTime(DateTime creationTime)
            {
                _internalData.CreationTime = creationTime;
            }

            private void SaveInternalData()
            {
                var client = _account.CreateCloudBlobClient();
                var container = client.GetContainerReference(JabbRSearchMetaDataContainer);

                container.CreateIfNotExists();

                CloudBlockBlob blockBlob = container.GetBlockBlobReference(JabbRSearchMetaDataFile);
                blockBlob.DeleteIfExists();

                using (var jsonWriter = new JsonTextWriter(new StreamWriter(blockBlob.OpenWrite())))
                {
                    var jsonSerializer = new JsonSerializer();
                    jsonSerializer.Serialize(jsonWriter, _internalData);
                }
            }

            private void ReloadInternalData()
            {
                // fetch from blob...
                var client = _account.CreateCloudBlobClient();
                var container = client.GetContainerReference(JabbRSearchMetaDataContainer);

                container.CreateIfNotExists();

                CloudBlockBlob blockBlob = container.GetBlockBlobReference(JabbRSearchMetaDataFile);
                if (blockBlob.Exists())
                {
                    using (var jsonReader = new JsonTextReader(new StreamReader(blockBlob.OpenRead())))
                    {
                        var jsonSerializer = new JsonSerializer();
                        var storageData = jsonSerializer.Deserialize<AzureBlobStorageIndexMetaData>(jsonReader);

                        if (storageData == null)
                        {
                            _internalData = new AzureBlobStorageIndexMetaData();
                        }
                        else
                        {
                            _internalData.LastMessageKey = storageData.LastMessageKey;
                            _internalData.LastWriteTime = storageData.LastWriteTime;
                            _internalData.CreationTime = storageData.CreationTime;
                        }
                    }
                }
            }

            private class AzureBlobStorageIndexMetaData
            {
                public int? LastMessageKey { get; set; }
                public DateTime? LastWriteTime { get; set; }
                public DateTime? CreationTime { get; set; }
            }
        }
    }
}