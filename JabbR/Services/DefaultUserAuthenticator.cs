using System.Collections.Generic;
using System.Security.Claims;
using JabbR.Infrastructure;
using JabbR.Models;

namespace JabbR.Services
{
    /// <summary>
    /// The default authenticator uses the username/password system in jabbr
    /// </summary>
    public class DefaultUserAuthenticator : IUserAuthenticator
    {
        private readonly IMembershipService _service;

        public DefaultUserAuthenticator(IMembershipService service)
        {
            _service = service;
        }

        public bool TryAuthenticateUser(string username, string password, out IList<Claim> claims)
        {
            claims = new List<Claim>();

            ChatUser user;
            if (_service.TryAuthenticateUser(username, password, out user))
            {
                claims.Add(new Claim(JabbRClaimTypes.Identifier, user.Id));

                // Add the admin claim if the user is an Administrator
                if (user.IsAdmin)
                {
                    claims.Add(new Claim(JabbRClaimTypes.Admin, "true"));
                }

                return true;
            }

            return false;
        }
    }
}