using Chat.Models;

namespace Chat.ViewModels
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
            Active = user.Active;
            Room = room == null ? null : room.Name;
        }

        public string Name { get; set; }
        public string Hash { get; set; }
        public string Id { get; set; }
        public bool Active { get; set; }
        public string Room { get; set; }
    }
}