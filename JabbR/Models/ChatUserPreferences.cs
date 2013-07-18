using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace JabbR.Models
{
    public class ChatUserPreferences
    {
        public static ChatUserPreferences GetPreferences(ChatUser chatUser)
        {
            var preferences = chatUser.RawPreferences != null ? JsonConvert.DeserializeObject<ChatUserPreferences>(chatUser.RawPreferences) : new ChatUserPreferences();

            // support migrating from versions of preferences with no tabOrder
            if (preferences.TabOrder == null)
            {
                preferences.TabOrder = new List<string> { "Lobby" };
                foreach (var room in chatUser.Rooms.Select(e => e.Name).OrderBy(e => e))
                {
                    preferences.TabOrder.Add(room);
                }
            }

            return preferences;
        }

        public void Serialize(ChatUser chatUser)
        {
            chatUser.RawPreferences = JsonConvert.SerializeObject(this);
        }

        public IList<string> TabOrder { get; set; }
    }
}
