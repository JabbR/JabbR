using System;
using Chat.Infrastructure;

namespace Chat.Models {
    public class ChatUser {
        public string ClientId { get; set; }
        public string Id { get; set; }
        public string Name { get; set; }
        public string Hash { get; set; }
        internal TimeSpan Offset { get; set; }
        internal TimeZoneInfo Timezone { get; set; }

        public ChatUser() {
        }

        public ChatUser(string name) {
            Name = name;
            Hash = name.ToMD5();
            Id = Guid.NewGuid().ToString("d");
        }

        public override bool Equals(object obj) {
            var other = obj as ChatUser;
            return other != null && Id.Equals(other.Id, StringComparison.OrdinalIgnoreCase);
        }

        public override int GetHashCode() {
            return Id.GetHashCode();
        }
    }
}