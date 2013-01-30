using System;
using JabbR.Models;

namespace JabbR.Services
{
    public interface IChatNotificationService
    {
        void OnUserNameChanged(ChatUser user, string oldUserName, string newUserName);
    }
}