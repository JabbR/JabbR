using System.Threading.Tasks;

namespace JabbR.ContentProviders
{
    public interface IResourceProcessor
    {
        Task<string> ExtractResource(string url);
    }
}
