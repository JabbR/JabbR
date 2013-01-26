using System;
using JabbR.Models;

namespace JabbR.Services
{
    public interface IAuthenticationTokenService : IDisposable
    {
        bool TryGetUserId(string authenticationToken, out string userId);
        string GetAuthenticationToken(ChatUser user);
    }
}