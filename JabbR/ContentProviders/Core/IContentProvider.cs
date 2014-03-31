using System;
using System.Threading.Tasks;

namespace JabbR.ContentProviders.Core
{
    public interface IContentProvider
    {
        Task<ContentProviderResult> GetContent(ContentProviderHttpRequest request);
        bool IsValidContent(Uri uri);
    }
}