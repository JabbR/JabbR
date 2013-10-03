using System;
using System.ComponentModel.Composition;
using System.IO;
using System.Threading.Tasks;
using JabbR.Services;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Ninject;

namespace JabbR.UploadHandlers
{
    public class AzureBlobStorageHandler : IUploadHandler
    {
        private readonly Func<ApplicationSettings> _settingsFunc;

        private const string JabbRUploadContainer = "jabbr-uploads";

        [ImportingConstructor]
        public AzureBlobStorageHandler(IKernel kernel)
        {
            _settingsFunc = () => kernel.Get<ApplicationSettings>();
        }

        public AzureBlobStorageHandler(ApplicationSettings settings)
        {
            _settingsFunc = () => settings;
        }

        public bool IsValid(string fileName, string contentType)
        {
            // Blob storage can handle any content
            return !String.IsNullOrEmpty(_settingsFunc().AzureblobStorageConnectionString);
        }

        public async Task<UploadResult> UploadFile(string fileName, string contentType, Stream stream)
        {
            var account = CloudStorageAccount.Parse(_settingsFunc().AzureblobStorageConnectionString);
            var client = account.CreateCloudBlobClient();
            var container = client.GetContainerReference(JabbRUploadContainer);

            // Randomize the filename everytime so we don't overwrite files
            string randomFile = Path.GetFileNameWithoutExtension(fileName) +
                                "_" +
                                Guid.NewGuid().ToString().Substring(0, 4) + Path.GetExtension(fileName);

            if (container.CreateIfNotExists())
            {
                // We need this to make files servable from blob storage
                container.SetPermissions(new BlobContainerPermissions
                {
                    PublicAccess = BlobContainerPublicAccessType.Blob
                });
            }

            CloudBlockBlob blockBlob = container.GetBlockBlobReference(randomFile);
            blockBlob.Properties.ContentType = contentType;

            await Task.Factory.FromAsync((cb, state) => blockBlob.BeginUploadFromStream(stream, cb, state), ar => blockBlob.EndUploadFromStream(ar), null);

            var result = new UploadResult
            {
                Url = blockBlob.Uri.ToString(),
                Identifier = randomFile
            };

            return result;
        }
    }
}