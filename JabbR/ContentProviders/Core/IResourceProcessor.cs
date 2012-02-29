using System.Threading.Tasks;

namespace JabbR.ContentProviders.Core
{
    public interface IResourceProcessor
    {
        Task<ContentProviderResult> ExtractResource(string url);
    }
}
