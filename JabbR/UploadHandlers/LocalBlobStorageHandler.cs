using System;
using System.ComponentModel.Composition;
using System.IO;
using System.Threading.Tasks;
using JabbR.Services;
using Ninject;

namespace JabbR.UploadHandlers
{
    public class LocalBlobStorageHandler : IUploadHandler
    {
        private readonly Func<ApplicationSettings> _settingsFunc;

        [ImportingConstructor]
        public LocalBlobStorageHandler(IKernel kernel)
        {
            _settingsFunc = () => kernel.Get<ApplicationSettings>();
        }

        public LocalBlobStorageHandler(ApplicationSettings settings)
        {
            _settingsFunc = () => settings;
        }

        public bool IsValid(string fileName, string contentType)
        {
            // Blob storage can handle any content
            return (!String.IsNullOrEmpty(_settingsFunc().LocalBlobStoragePath) &&
                    !String.IsNullOrEmpty(_settingsFunc().LocalBlobStorageUriPrefix));
        }

        public async Task<UploadResult> UploadFile(string fileName, string contentType, Stream stream)
        {
            // Randomize the filename everytime so we don't overwrite files
            string randomFile = Path.GetFileNameWithoutExtension(fileName) +
                                "_" +
                                Guid.NewGuid().ToString().Substring(0, 4) + Path.GetExtension(fileName);

            if (!Directory.Exists(_settingsFunc().LocalBlobStoragePath))
            {
                Directory.CreateDirectory(_settingsFunc().LocalBlobStoragePath);
            }


            using (
                FileStream destinationStream =
                    File.Create(Path.Combine(_settingsFunc().LocalBlobStoragePath, randomFile)))
            {
                await stream.CopyToAsync(destinationStream);
            }


            var result = new UploadResult
            {
                Url = (new Uri(_settingsFunc().LocalBlobStorageUriPrefix + randomFile)).ToString(),
                Identifier = randomFile
            };

            return result;
        }
    }
}