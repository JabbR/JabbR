using System.ComponentModel.Composition;
using System.Net;

namespace JabbR
{
    [InheritedExport]
    public interface IContentProvider
    {
        string GetContent(HttpWebResponse response);
    }
}