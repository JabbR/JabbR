using System;
using JabbR.Models;

namespace JabbR.Services
{
    public interface IAuthenticationTokenService
    {
        bool TryGetUserId(string authenticationToken, out string userId);
        string GetAuthenticationToken(ChatUser user);
    }
}