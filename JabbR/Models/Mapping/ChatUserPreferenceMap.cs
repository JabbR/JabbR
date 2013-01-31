using System.ComponentModel.DataAnnotations;
using System.Data.Entity.ModelConfiguration;

namespace JabbR.Models.Mapping
{
    public class ChatUserPreferenceMap : EntityTypeConfiguration<ChatUserPreference>
    {
        public ChatUserPreferenceMap()
        {
            // Primary Key
            this.HasKey(c => new { c.ChatUserId, c.RoomId, c.Key });

            // Properties
            // Table & Column Mappings
            this.ToTable("ChatUserPreferences");
            this.Property(c => c.Key).HasColumnName("Key");
            this.Property(c => c.ChatUserId).HasColumnName("ChatUserId");
            this.Property(c => c.RoomId).HasColumnName("RoomId");
            this.Property(c => c.Value).HasColumnName("Value");

            // Relationships
            this.HasRequired(c => c.User)
                .WithMany(u => u.Preferences)
                .HasForeignKey(c => c.ChatUserId);

            this.HasRequired(c => c.Room)
                .WithMany()
                .HasForeignKey(c => c.RoomId);
        }
    }
}