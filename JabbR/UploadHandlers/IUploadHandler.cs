using System.ComponentModel.Composition;
using System.IO;
using System.Threading.Tasks;

namespace JabbR.UploadHandlers
{
    [InheritedExport]
    public interface IUploadHandler
    {
        bool IsValid(string fileName, string contentType);
        Task<string> UploadFile(string fileName, string contentType, Stream stream);
    }
}