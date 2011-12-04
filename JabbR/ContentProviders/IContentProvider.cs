using System.ComponentModel.Composition;
using System.Net;
using JabbR.ContentProviders;

namespace JabbR
{
    [InheritedExport]
    public interface IContentProvider
    {
        ContentProviderResultModel GetContent(HttpWebResponse response);
    }
}