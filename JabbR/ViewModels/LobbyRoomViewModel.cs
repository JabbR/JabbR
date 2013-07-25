using System;
using System.Collections.Generic;
using JabbR.Models;

namespace JabbR.ViewModels
{
    public class LobbyRoomViewModel
    {
        public string Name { get; set; }
        public int Count { get; set; }
        public RoomType RoomType { get; set; }
        public bool Closed { get; set; }
        public string Topic { get; set; }
    }
}