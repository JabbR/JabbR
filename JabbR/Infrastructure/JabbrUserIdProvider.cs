using Microsoft.AspNet.SignalR;

namespace JabbR.Infrastructure
{
    public class JabbrUserIdProvider : IUserIdProvider
    {
        public string GetUserId(IRequest request)
        {
            if (request.User == null)
            {
                return null;
            }

            return request.User.GetUserId();
        }
    }
}