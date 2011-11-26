using JabbR.Models;

namespace JabbR.ViewModels
{
    public class UserViewModel
    {
        public UserViewModel(ChatUser user) : this (user, null)
        {
        }

        public UserViewModel(ChatUser user, ChatRoom room)
        {
            Name = user.Name;
            Hash = user.Hash;
            Id = user.Id;
            Active = user.Status == (int)UserStatus.Active;
            Room = room == null ? null : room.Name;
            IsOwner = room == null ? false : room.Owner == user;
        }

        public string Name { get; set; }
        public string Hash { get; set; }
        public string Id { get; set; }
        public bool Active { get; set; }

        // REVIEW: These don't belong in this view model
        public string Room { get; set; }
        public bool IsOwner { get; set; }
    }
}