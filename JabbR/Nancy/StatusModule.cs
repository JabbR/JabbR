using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Linq;
using JabbR.Services;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Infrastructure;
using Microsoft.AspNet.SignalR.Messaging;
using Nancy;

namespace JabbR.Nancy
{
    public class StatusModule : JabbRModule
    {
        public StatusModule(IMessageBus messageBus, IConnectionManager connectionManager, IJabbrRepository jabbrRepository)
            : base("/status")
        {
            Get["/"] = _ =>
            {
                // Try to send a message
                var hubContext = connectionManager.GetHubContext<Chat>();
                var sendTask = (Task)hubContext.Clients.Client("doesn't exist").noMethodCalledThis();
                sendTask.Wait();

                // Try to talk to database
                var roomCount = jabbrRepository.Rooms.Count();

                return View["index"];
            };
        }
    }
}