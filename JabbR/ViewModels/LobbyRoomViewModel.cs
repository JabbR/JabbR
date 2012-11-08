using System.Collections.Generic;

namespace JabbR.ViewModels
{
    public class LobbyRoomViewModel
    {
        public string Name { get; set; }
        public int Count { get; set; }
        public bool Private { get; set; }
        public bool Closed { get; set; }
    }
}