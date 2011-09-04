using Chat.Models;

namespace Chat.ViewModels {
    public class UserViewModel {
        public UserViewModel(ChatUser user) {
            Name = user.Name;
            Hash = user.Hash;
            Id = user.Id;
            Active = user.Active;
        }

        public string Name { get; set; }
        public string Hash { get; set; }
        public string Id { get; set; }
        public bool Active { get; set; }
    }
}