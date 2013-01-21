using System;
using JabbR.Models;
using Microsoft.AspNet.SignalR.Infrastructure;

namespace JabbR.Services
{
    public class AuthenticationService : IAuthenticationService
    {
        private readonly IJabbrRepository _repository;

        // What a hack: This comes from SignalR (Won't work on .NET 4.0)
        private readonly IProtectedData _protectedData;

        public AuthenticationService(IJabbrRepository repository, IProtectedData protectedData)
        {
            _repository = repository;
            _protectedData = protectedData;
        }

        public bool TryGetUserId(string userToken, out string userId)
        {
            userId = _protectedData.Unprotect(userToken, "userId");

            if (_repository.GetUserById(userId) != null)
            {
                return true;
            }

            return false;
        }

        public bool IsUserAuthenticated(string userToken)
        {
            string userId = _protectedData.Unprotect(userToken, "userId");
            return _repository.GetUserById(userId) != null;
        }

        public string GetAuthenticationToken(ChatUser user)
        {
            return _protectedData.Protect(user.Id, "userId");
        }

        public void Dispose()
        {
            _repository.Dispose();
        }
    }
}