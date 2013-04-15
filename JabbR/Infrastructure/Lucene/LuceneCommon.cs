using System;
using System.IO;
using LuceneVersion = Lucene.Net.Util.Version;
using LuceneDir = Lucene.Net.Store.Directory;

namespace JabbR.Infrastructure
{
    internal static class LuceneCommon
    {
        internal static readonly string IndexDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "App_Data/Lucene");
        internal static readonly string IndexMetadataPath = Path.Combine(IndexDirectory ?? ".", "index.metadata");
        internal static readonly LuceneVersion LuceneVersion = LuceneVersion.LUCENE_30;

        private static LuceneDir _directorySingleton;

        internal static LuceneDir GetDirectory()
        {
            if (_directorySingleton == null)
            {
                if (!Directory.Exists(IndexDirectory))
                {
                    Directory.CreateDirectory(IndexDirectory);
                }

                var directoryInfo = new DirectoryInfo(IndexDirectory);
                _directorySingleton = new Lucene.Net.Store.SimpleFSDirectory(directoryInfo);
            }

            return _directorySingleton;
        }
    }
}