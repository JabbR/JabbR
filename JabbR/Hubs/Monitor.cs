using JabbR.Infrastructure;
using Microsoft.AspNet.SignalR;

namespace JabbR.Hubs
{
    [AuthorizeClaim(JabbRClaimTypes.Admin)]
    public class Monitor : Hub
    {
    }
}