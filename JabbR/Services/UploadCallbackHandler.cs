using System.IO;
using JabbR.ContentProviders.Core;
using JabbR.UploadHandlers;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Infrastructure;

namespace JabbR.Services
{
    public class UploadCallbackHandler
    {
        private readonly UploadProcessor _processor;
        private readonly IResourceProcessor _resourceProcessor;
        private readonly IHubContext _hubContext;
        private readonly IChatService _service;

        public UploadCallbackHandler(UploadProcessor processor,
                                     IResourceProcessor resourceProcessor,
                                     IConnectionManager connectionManager,
                                     IChatService service)
        {
            _processor = processor;
            _resourceProcessor = resourceProcessor;
            _hubContext = connectionManager.GetHubContext<Chat>();
            _service = service;
        }

        public async void Upload(string userId, string connectionId, string roomName, string clientMessageId, string file, string contentType, Stream stream)
        {
            string contentUrl = await _processor.HandleUpload(file, contentType, stream);

            if (contentUrl == null)
            {
                _hubContext.Clients.Client(connectionId).postMessage("Failed to upload " + Path.GetFileName(file) + ".", "error", roomName);
                return;
            }

            // Add the message to the persistent chat
            _service.AddMessage(userId, roomName, contentUrl);

            // Notify all clients for the uploaded url
            _hubContext.Clients.Group(roomName).appendMessage(clientMessageId, contentUrl, roomName);

            // Run the content providers (I wish this happened client side :))
            Chat.ProcessUrls(new[] { contentUrl }, _hubContext.Clients, _resourceProcessor, roomName, clientMessageId);
        }
    }
}