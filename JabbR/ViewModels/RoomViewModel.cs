using System.Collections.Generic;

namespace JabbR.ViewModels
{
    public class RoomViewModel
    {
        public string Name { get; set; }
        public int Count { get; set; }
        public bool Private { get; set; }
        public string Topic { get; set; }
        public string Welcome { get; set; }
        public IEnumerable<UserViewModel> Users { get; set; }
        public IEnumerable<string> Owners { get; set; }
        public IEnumerable<MessageViewModel> RecentMessages { get; set; }
    }
}