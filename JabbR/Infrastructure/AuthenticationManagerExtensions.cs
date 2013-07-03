using System.Security.Claims;
using Microsoft.Owin.Security;

namespace JabbR.Infrastructure
{
    public static class AuthenticationManagerExtensions
    {
        public static void SignIn(this IAuthenticationManager authenticationManager, ClaimsIdentity identity)
        {
            var extra = new AuthenticationExtra();
            authenticationManager.SignIn(extra, identity);
        }
    }
}