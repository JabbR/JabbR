using System;
using System.ComponentModel.Composition;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using JabbR.ContentProviders;
using JabbR.Services;
using Newtonsoft.Json.Linq;

namespace JabbR.UploadHandlers
{
    public class ImagurUploadHandler : IUploadHandler
    {
        private readonly IApplicationSettings _settings;

        [ImportingConstructor]
        public ImagurUploadHandler(IApplicationSettings settings)
        {
            _settings = settings;
        }

        public bool IsValid(string fileName, string contentType)
        {
            return ImageContentProvider.IsValidContentType(contentType);
        }

        public async Task<string> UploadFile(string fileName, Stream stream)
        {
            if (String.IsNullOrEmpty(_settings.ImagurClientId))
            {
                return null;
            }

            var client = new HttpClient();

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Client-ID", _settings.ImagurClientId);

            var content = new StreamContent(stream);

            var response = await client.PostAsync("https://api.imgur.com/3/upload", content);

            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            JObject obj = JObject.Parse(await response.Content.ReadAsStringAsync());

            return obj["data"].Value<string>("link");
        }
    }
}