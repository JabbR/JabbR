using System;
using JabbR.Models;
using JabbR.Services;

namespace JabbR.ViewModels
{
    public class UserViewModel
    {
        public UserViewModel(ChatUser user)
        {
            Name = user.Name;
            Hash = user.Hash;
            Active = user.Status == (int)UserStatus.Active;
            Status = ((UserStatus)user.Status).ToString();
            Note = user.Note;
            AfkNote = user.AfkNote;
            IsAfk = user.IsAfk;
            Flag = user.Flag;
            Country = ChatService.GetCountry(user.Flag);
            LastActivity = user.LastActivity;
            IsAdmin = user.IsAdmin;
        }

        public string Name { get; private set; }
        public string Hash { get; private set; }
        public bool Active { get; private set; }
        public string Status { get; private set; }
        public string Note { get; private set; }
        public string AfkNote { get; private set; }
        public bool IsAfk { get; private set; }
        public string Flag { get; private set; }
        public string Country { get; private set; }
        public DateTime LastActivity { get; private set; }
        public bool IsAdmin { get; private set; }
    }
}