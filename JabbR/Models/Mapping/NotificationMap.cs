using System.Data.Entity.ModelConfiguration;

namespace JabbR.Models.Mapping
{
    public class NotificationMap : EntityTypeConfiguration<Notification>
    {
        public NotificationMap()
        {
            // Primary Key
            this.HasKey(m => m.Key);

            // Properties
            // Table & Column Mappings
            this.ToTable("Notifications");
            this.Property(m => m.Key).HasColumnName("Key");
            this.Property(m => m.Read).HasColumnName("Read");

            this.HasRequired(a => a.Message)
                .WithMany(u => u.Notifications)
                .HasForeignKey(a => a.MessageKey);

            this.HasRequired(a => a.User)
                .WithMany(u => u.Notifications)
                .HasForeignKey(a => a.UserKey);
        }
    }
}