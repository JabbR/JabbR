using System.Security.Principal;

namespace JabbR.Models
{
    public class JabbRIdentity : GenericIdentity
    {
        public JabbRIdentity(string userId)
            : base(name: null)
        {
            UserId = userId;
        }

        public string UserId { get; set; }
    }
}