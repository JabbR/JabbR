using System.Net;

namespace JabbR
{
    public interface IContentProvider
    {
        string GetContent(HttpWebResponse response);
    }
}