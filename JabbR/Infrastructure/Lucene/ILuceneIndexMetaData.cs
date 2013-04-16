using System;

namespace JabbR.Infrastructure
{
    public interface ILuceneIndexMetaData
    {
        int? LastMessageKey { get; }
        DateTime? LastWriteTime { get; }
        DateTime? CreationTime { get; }

        bool RequiresRefresh();
        void UpdateLastWriteTime(DateTime lastWriteTime, int lastMessageKey);
        void UpdateCreationTime(DateTime creationTime);
    }
}