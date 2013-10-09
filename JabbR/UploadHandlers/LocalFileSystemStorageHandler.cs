using System;
using System.ComponentModel.Composition;
using System.IO;
using System.Security;
using System.Threading.Tasks;
using JabbR.Services;
using Ninject;

namespace JabbR.UploadHandlers
{
    public class LocalFileSystemStorageHandler : IUploadHandler
    {
        private readonly Func<ApplicationSettings> _settingsFunc;

        [ImportingConstructor]
        public LocalFileSystemStorageHandler(IKernel kernel)
        {
            _settingsFunc = () => kernel.Get<ApplicationSettings>();
        }

        public LocalFileSystemStorageHandler(ApplicationSettings settings)
        {
            _settingsFunc = () => settings;
        }

        public bool IsValid(string fileName, string contentType)
        {
            var settings = _settingsFunc();

            // Blob storage can handle any content
            return (!String.IsNullOrEmpty(settings.LocalFileSystemStoragePath) &&
                    !String.IsNullOrEmpty(settings.LocalFileSystemStorageUriPrefix));
        }

        public async Task<UploadResult> UploadFile(string fileName, string contentType, Stream stream)
        {
            var settings = _settingsFunc();

            // Randomize the filename everytime so we don't overwrite files
            string randomFile = Guid.NewGuid().ToString();

            if (!Directory.Exists(settings.LocalFileSystemStoragePath))
            {
                Directory.CreateDirectory(settings.LocalFileSystemStoragePath);
            }

            var targetFile = Path.Combine(settings.LocalFileSystemStoragePath, randomFile);

            using (FileStream destinationStream = File.Create(targetFile))
            {
                await stream.CopyToAsync(destinationStream);
            }


            var result = new UploadResult
            {
                Url = (new Uri(settings.LocalFileSystemStorageUriPrefix + randomFile)).ToString(),
                Identifier = randomFile
            };

            return result;
        }
    }
}