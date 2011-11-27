using System.Linq;
using System.Collections.Generic;

namespace JabbR.Models
{
    public static class RepositoryExtensions
    {
        public static IQueryable<ChatUser> Online(this IQueryable<ChatUser> source)
        {
            return source.Where(u => u.Status != (int)UserStatus.Offline);
        }

        public static IEnumerable<ChatUser> Online(this IEnumerable<ChatUser> source)
        {
            return source.Where(u => u.Status != (int)UserStatus.Offline);
        }

        public static ChatUser GetUserByClientId(this IJabbrRepository repository, string clientId)
        {
            return repository.Users.FirstOrDefault(u => u.ClientId == clientId);
        }
    }
}