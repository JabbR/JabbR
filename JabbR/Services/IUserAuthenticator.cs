using System.Collections.Generic;
using System.Security.Claims;

namespace JabbR.Services
{
    public interface IUserAuthenticator
    {
        bool TryAuthenticateUser(string username, string password, out IList<Claim> claims);
    }
}