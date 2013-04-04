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
        private readonly ContentProviderProcessor _resourceProcessor;
        private readonly IHubContext _hubContext;
        private readonly IChatService _service;
        private readonly IApplicationSettings _settings;

        public UploadCallbackHandler(UploadProcessor processor,
                                     ContentProviderProcessor resourceProcessor,
                                     IConnectionManager connectionManager,
                                     IChatService service,
                                     IApplicationSettings settings)
        {
            _processor = processor;
            _resourceProcessor = resourceProcessor;
            _hubContext = connectionManager.GetHubContext<Chat>();
            _service = service;
            _settings = settings;
        }

        public async Task Upload(string userId,
                                 string connectionId,
                                 string roomName,
                                 string file,
                                 string contentType,
                                 Stream stream)
        {

            if (stream.Length > _settings.MaxFileUploadBytes)
            {
                _hubContext.Clients.Client(connectionId).postMessage("Unable to upload " + Path.GetFileName(file) + " because it exceeded the maximum size allowed.", "error", roomName);
                return;
            }

            UploadResult result;
            ChatMessage message;

            try
            {
                result = await _processor.HandleUpload(file, contentType, stream);

                if (result == null)
                {
                    _hubContext.Clients.Client(connectionId).postMessage("Failed to upload " + Path.GetFileName(file) + ".", "error", roomName);
                    return;
                }

                // Add the message to the persistent chat
                message = _service.AddMessage(userId, roomName, result.Url);

                // Keep track of this attachment
                _service.AddAttachment(message, file, contentType, stream.Length, result);
            }
            catch (Exception ex)
            {
                _hubContext.Clients.Client(connectionId).postMessage("Failed to upload " + Path.GetFileName(file) + ". " + ex.Message, "error", roomName);
                return;
            }

            var messageViewModel = new MessageViewModel(message);

            // Notify all clients for the uploaded url
            _hubContext.Clients.Group(roomName).addMessage(messageViewModel, roomName);

            _resourceProcessor.ProcessUrls(new[] { result.Url }, _hubContext.Clients, roomName, message.Id);
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