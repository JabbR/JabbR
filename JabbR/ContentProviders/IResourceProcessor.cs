using System.Threading.Tasks;
using JabbR.Models;

namespace JabbR.ContentProviders
{
    public interface IResourceProcessor
    {
        Task<ContentProviderResultModel> ExtractResource(string url);
    }
}
