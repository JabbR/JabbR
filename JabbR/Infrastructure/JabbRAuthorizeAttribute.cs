using System.Security.Principal;
using Microsoft.AspNet.SignalR;

namespace JabbR.Infrastructure
{
    public class JabbRAuthorizeAttribute : AuthorizeAttribute
    {
        protected override bool UserAuthorized(IPrincipal user)
        {
            return user.IsAuthenticated();
        }
    }
}