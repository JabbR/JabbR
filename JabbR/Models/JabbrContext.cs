using System.Data.Entity;
using JabbR.Models.Mapping;

namespace JabbR.Models
{
    public class JabbrContext : DbContext
    {
        public JabbrContext()
            : base("Jabbr")
        {
        }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Configurations.Add(new ChatClientMap());

            modelBuilder.Configurations.Add(new ChatMessageMap());

            modelBuilder.Configurations.Add(new ChatRoomMap());

            modelBuilder.Configurations.Add(new ChatUserMap());

            modelBuilder.Configurations.Add(new ChatUserPreferenceMap());

            base.OnModelCreating(modelBuilder);
        }

        public DbSet<ChatClient> Clients { get; set; }
        public DbSet<ChatMessage> Messages { get; set; }
        public DbSet<ChatRoom> Rooms { get; set; }
        public DbSet<ChatUser> Users { get; set; }
        public DbSet<ChatUserIdentity> Identities { get; set; }
        public DbSet<ChatUserPreference> Preferences { get; set; }
    }
}