using System;
using JabbR.Infrastructure;
using Microsoft.AspNet.SignalR.Hubs;

namespace JabbR.Hubs
{
    public class LoggingHubPipelineModule : HubPipelineModule
    {
        private readonly ILogger _logger;

        public LoggingHubPipelineModule(ILogger logger)
        {
            _logger = logger;
        }

        protected override void OnIncomingError(Exception ex, IHubIncomingInvokerContext context)
        {
            _logger.LogError("{0}: Failure while invoking '{1}'.", context.Hub.Context.Request.User.GetUserId(), context.MethodDescriptor.Name);
            _logger.Log(ex);
        }
    }
}