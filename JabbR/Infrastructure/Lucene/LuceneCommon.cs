using System;
using System.IO;
using LuceneVersion = Lucene.Net.Util.Version;

namespace JabbR.Infrastructure
{
    internal static class LuceneCommon
    {
        internal static readonly LuceneVersion LuceneVersion = LuceneVersion.LUCENE_30;
    }
}