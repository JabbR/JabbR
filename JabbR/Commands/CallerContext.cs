using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace JabbR.Commands
{
    public class CallerContext
    {
        public string RoomName { get; set; }
        public string ClientId { get; set; }
        public string UserId { get; set; }
        public string UserAgent { get; set; }
    }
}