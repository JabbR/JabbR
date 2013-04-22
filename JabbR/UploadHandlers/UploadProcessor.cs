using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using JabbR.Services;

namespace JabbR.UploadHandlers
{
    public class UploadProcessor
    {
        private readonly IList<IUploadHandler> _fileUploadHandlers;

        public UploadProcessor(ApplicationSettings settings)
        {
            _fileUploadHandlers = GetUploadHandlers(settings);
        }

        public async Task<UploadResult> HandleUpload(string fileName, string contentType, Stream stream)
        {
            IUploadHandler handler = _fileUploadHandlers.FirstOrDefault(c => c.IsValid(fileName, contentType));

            if (handler == null)
            {
                return null;
            }

            return await handler.UploadFile(fileName, contentType, stream);
        }

        private static IList<IUploadHandler> GetUploadHandlers(ApplicationSettings settings)
        {
            // Use MEF to locate the content providers in this assembly
            var compositionContainer = new CompositionContainer(new AssemblyCatalog(typeof(UploadProcessor).Assembly));
            compositionContainer.ComposeExportedValue<ApplicationSettings>(settings);
            return compositionContainer.GetExportedValues<IUploadHandler>().ToList();
        }
    }
}