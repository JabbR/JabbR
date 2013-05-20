using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using JabbR.Services;
using Ninject;

namespace JabbR.UploadHandlers
{
    public class UploadProcessor
    {
        private readonly IKernel _kernel;
        private readonly IList<IUploadHandler> _fileUploadHandlers;

        public UploadProcessor(IKernel kernel)
        {
            _kernel = kernel;
            _fileUploadHandlers = GetUploadHandlers(kernel);
        }

        public async Task<UploadResult> HandleUpload(string fileName, string contentType, Stream stream, long contentLength)
        {
            var settings = _kernel.Get<ApplicationSettings>();

            if (contentLength > settings.MaxFileUploadBytes)
            {
                return null;
            }
            
            IUploadHandler handler = _fileUploadHandlers.FirstOrDefault(c => c.IsValid(fileName, contentType));

            if (handler == null)
            {
                return null;
            }

            return await handler.UploadFile(fileName, contentType, stream);
        }

        private static IList<IUploadHandler> GetUploadHandlers(IKernel kernel)
        {
            // Use MEF to locate the content providers in this assembly
            var compositionContainer = new CompositionContainer(new AssemblyCatalog(typeof(UploadProcessor).Assembly));
            compositionContainer.ComposeExportedValue<IKernel>(kernel);
            return compositionContainer.GetExportedValues<IUploadHandler>().ToList();
        }
    }
}