using System;
using System.Diagnostics;
using System.Threading.Tasks;
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
            // Fire and forget
            Task.Run(async () =>
            {
                var formatted = String.Format("[{0}]: {1}", DateTime.UtcNow, message);

                try
                {
                    switch (type)
                    {
                        case LogType.Message:
                            await _logContext.Clients.All.logMessage(formatted);
                            break;
                        case LogType.Error:
                            await _logContext.Clients.All.logError(formatted);
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Trace.TraceError("Error occurred while logging: " + ex);
                }
            });
        }
    }
}