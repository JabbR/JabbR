using System;
using System.IO;
using Lucene.Net.Store;
using Directory = Lucene.Net.Store.Directory;

namespace JabbR.Infrastructure
{
    public class LuceneLocalFileSystem : ILuceneFileSystem
    {
        internal static readonly string IndexDirectoryPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"App_Data\\Lucene");
        internal static readonly string IndexMetadataPath = Path.Combine(IndexDirectoryPath ?? ".", "index.metadata");
        private static readonly TimeSpan IndexRecreateInterval = TimeSpan.FromDays(3);

        private SimpleFSDirectory _indexDir;
        public Directory IndexDirectory
        {
            get
            {
                if (_indexDir == null)
                {
                    if (!System.IO.Directory.Exists(IndexDirectoryPath))
                    {
                        System.IO.Directory.CreateDirectory(IndexDirectoryPath);
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
            public int? LastMessageKey
            {
                get
                {
                    if (!File.Exists(IndexMetadataPath))
                    {
                        return null;
                    }

                    int lastMessageKey;
                    if (Int32.TryParse(File.ReadAllLines(IndexMetadataPath)[0].Trim(), out lastMessageKey))
                    {
                        return lastMessageKey;
                    }

                    return null;
                }
            }

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

            public void UpdateLastWriteTime(DateTime lastWriteTime, int lastMessageKey)
            {
                File.WriteAllLines(IndexMetadataPath, new[] { lastMessageKey.ToString() });
                File.SetLastWriteTimeUtc(IndexMetadataPath, lastWriteTime);
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
}