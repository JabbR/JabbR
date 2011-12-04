using System.ComponentModel.Composition;
using System.Net;
using JabbR.Models;

namespace JabbR
{
    [InheritedExport]
    public interface IContentProvider
    {
        ContentProviderResultModel GetContent(HttpWebResponse response);
    }
}