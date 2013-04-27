using System;
using JabbR.Hubs;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Infrastructure;

namespace JabbR.Infrastructure
{
    public class RealtimeLogger : ILogger
    {
        private readonly IHubContext _logContext;

        public RealtimeLogger(IConnectionManager connectionManager)
        {
            _logContext = connectionManager.GetHubContext<Monitor>();
        }

        public void Log(LogType type, string message)
        {
            message = String.Format("[{0}]: {1}", DateTime.UtcNow, message);

            switch (type)
            {
                case LogType.Message:
                    _logContext.Clients.All.logMessage(message);
                    break;
                case LogType.Error:
                    _logContext.Clients.All.logError(message);
                    break;
            }
        }
    }
}