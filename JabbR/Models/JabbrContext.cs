using System.Data.Entity;

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
            modelBuilder.Entity<ChatRoom>()
                        .HasOptional(r => r.Creator);

            modelBuilder.Entity<ChatRoom>()
                        .HasMany(r => r.Owners)
                        .WithMany(u => u.OwnedRooms)
                        .Map(c => c.ToTable("ChatRoomChatUsers"));

            modelBuilder.Entity<ChatRoom>()
                        .HasMany(r => r.AllowedUsers)
                        .WithMany(u => u.AllowedRooms)
                        .Map(c => c.ToTable("ChatRoomChatUser1")); 
            
            modelBuilder.Entity<ChatUser>()
                        .HasMany(u => u.Rooms)
                        .WithMany(r => r.Users)
                        .Map(c => c.ToTable("ChatUserChatRooms"));

            modelBuilder.Entity<ChatUser>()
                        .HasMany(u => u.ConnectedClients);

            modelBuilder.Entity<ChatClient>()
                        .HasRequired(c => c.User);

            base.OnModelCreating(modelBuilder);
        }

        public DbSet<ChatClient> Clients { get; set; }
        public DbSet<ChatMessage> Messages { get; set; }
        public DbSet<ChatRoom> Rooms { get; set; }
        public DbSet<ChatUser> Users { get; set; }
    }
}