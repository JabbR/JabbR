using System.ComponentModel.DataAnnotations;
using System.Data.Entity.ModelConfiguration;

namespace JabbR.Models.Mapping
{
    public class ChatMessageMap : EntityTypeConfiguration<ChatMessage>
    {
        public ChatMessageMap()
        {
            // Primary Key
            this.HasKey(m => m.Key);

            // Properties
            // Table & Column Mappings
            this.ToTable("ChatMessages");
            this.Property(m => m.Key).HasColumnName("Key");
            this.Property(m => m.Content).HasColumnName("Content");
            this.Property(m => m.Id).HasColumnName("Id");
            this.Property(m => m.When).HasColumnName("When");
            this.Property(m => m.RoomKey).HasColumnName("Room_Key");
            this.Property(m => m.UserKey).HasColumnName("User_Key");

            // Relationships
            this.HasOptional(m => m.Room)
                .WithMany(r => r.Messages)
                .HasForeignKey(m => m.RoomKey);

            this.HasOptional(m => m.User)
                .WithMany()
                .HasForeignKey(m => m.UserKey);

        }
    }
}