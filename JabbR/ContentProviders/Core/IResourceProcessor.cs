using System.Threading.Tasks;

namespace JabbR.ContentProviders.Core
{
    public interface IResourceProcessor
    {
        Task<ContentProviderResultModel> ExtractResource(string url);
    }
}
