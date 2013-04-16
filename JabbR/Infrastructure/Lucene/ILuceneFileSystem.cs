using LuceneDirectory = Lucene.Net.Store.Directory;

namespace JabbR.Infrastructure
{
    public interface ILuceneFileSystem
    {
        LuceneDirectory IndexDirectory { get; }
        ILuceneIndexMetaData MetaData { get; }
    }
}
