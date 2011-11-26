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
                        .HasOptional(r => r.Owner);
            
            modelBuilder.Entity<ChatUser>()
                        .HasMany(u => u.Rooms).WithMany(r => r.Users);
            
            base.OnModelCreating(modelBuilder);
        }

        public DbSet<ChatRoom> Rooms { get; set; }
        public DbSet<ChatUser> Users { get; set; }
    }
}