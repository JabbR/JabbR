using System.Threading.Tasks;

namespace JabbR.ContentProviders
{
    public interface IResourceProcessor
    {
        Task<ContentProviderResultModel> ExtractResource(string url);
    }
}
