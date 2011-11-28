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
                        .HasMany(r => r.Owners).WithMany(r => r.OwnedRooms);
            
            modelBuilder.Entity<ChatUser>()
                        .HasMany(u => u.Rooms).WithMany(r => r.Users);
            
            base.OnModelCreating(modelBuilder);
        }

        public DbSet<ChatRoom> Rooms { get; set; }
        public DbSet<ChatUser> Users { get; set; }
    }
}