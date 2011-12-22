using System;
using JabbR.Models;

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
            IsAfk = user.IsAfk;
            LastActivity = user.LastActivity;
        }

        public string Name { get; set; }
        public string Hash { get; set; }
        public bool Active { get; set; }
        public string Status { get; set; }
        public string Note { get; set; }
        public bool IsAfk { get; set; }
        public DateTime LastActivity { get; set; }
    }
}