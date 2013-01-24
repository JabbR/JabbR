using System;
using JabbR.Models;

namespace JabbR.Services
{
    public interface IAuthenticationTokenService : IDisposable
    {
        bool IsValidAuthenticationToken(string authenticationToken);
        bool TryGetUserId(string authenticationToken, out string userId);
        string GetAuthenticationToken(ChatUser user);
    }
}