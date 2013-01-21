using System;
using JabbR.Models;

namespace JabbR.Services
{
    public interface IAuthenticationService : IDisposable
    {
        bool IsUserAuthenticated(string userToken);
        bool TryGetUserId(string userToken, out string userId);
        string GetAuthenticationToken(ChatUser user);
    }
}