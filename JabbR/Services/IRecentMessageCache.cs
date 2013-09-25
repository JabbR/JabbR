using System.Collections.Generic;
using JabbR.Models;
using JabbR.ViewModels;

namespace JabbR.Services
{
    public interface IRecentMessageCache
    {
        void Add(ChatMessage message);

        void Add(string room, ICollection<MessageViewModel> messages);

        ICollection<MessageViewModel> GetRecentMessages(string roomName);
    }
}
