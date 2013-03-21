using System;
using System.IO;
using System.Threading.Tasks;
using JabbR.ContentProviders.Core;
using JabbR.Models;
using JabbR.UploadHandlers;
using JabbR.ViewModels;
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

        public async Task Upload(string userId,
                                 string connectionId,
                                 string roomName,
                                 string file,
                                 string contentType,
                                 Stream stream)
        {
            string contentUrl = null;

            try
            {
                contentUrl = await _processor.HandleUpload(file, contentType, stream);

                if (contentUrl == null)
                {
                    _hubContext.Clients.Client(connectionId).postMessage("Failed to upload " + Path.GetFileName(file) + ".", "error", roomName);
                    return;
                }
            }
            catch (Exception ex)
            {
                _hubContext.Clients.Client(connectionId).postMessage("Failed to upload " + Path.GetFileName(file) + ". " + ex.Message, "error", roomName);
                return;
            }

            string content = String.Format("{0} ({1}) {2}", Path.GetFileName(file), contentUrl, FormatBytes(stream.Length));

            // Add the message to the persistent chat
            ChatMessage message = _service.AddMessage(userId, roomName, content);

            var messageViewModel = new MessageViewModel(message);

            // Notify all clients for the uploaded url
            _hubContext.Clients.Group(roomName).addMessage(messageViewModel, roomName);

            // Run the content providers (I wish this happened client side :))
            Chat.ProcessUrls(new[] { contentUrl }, _hubContext.Clients, _resourceProcessor, roomName, message.Id);
        }

        private static string FormatBytes(long bytes)
        {
            const int scale = 1024;
            string[] orders = new string[] { "GB", "MB", "KB", "Bytes" };
            long max = (long)Math.Pow(scale, orders.Length - 1);

            foreach (string order in orders)
            {
                if (bytes > max)
                {
                    return String.Format("{0:##.##} {1}", Decimal.Divide(bytes, max), order);
                }

                max /= scale;
            }
            return "0 Bytes";
        }
    }
}