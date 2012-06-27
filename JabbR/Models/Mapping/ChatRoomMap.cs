using System.ComponentModel.DataAnnotations;
using System.Data.Entity.ModelConfiguration;

namespace JabbR.Models.Mapping
{
    public class ChatRoomMap : EntityTypeConfiguration<ChatRoom>
    {
        public ChatRoomMap()
        {
            // Primary Key
            this.HasKey(r => r.Key);

            // Properties
            this.Property(r => r.InviteCode)
                .IsFixedLength()
                .HasMaxLength(6);

            this.Property(r => r.Topic)
                .HasMaxLength(80);

            // Table & Column Mappings
            this.ToTable("ChatRooms");
            this.Property(r => r.Key).HasColumnName("Key");
            this.Property(r => r.LastNudged).HasColumnName("LastNudged");
            this.Property(r => r.Name).HasColumnName("Name");
            this.Property(r => r.CreatorKey).HasColumnName("Creator_Key");
            this.Property(r => r.Private).HasColumnName("Private");
            this.Property(r => r.InviteCode).HasColumnName("InviteCode");
            this.Property(r => r.Closed).HasColumnName("Closed");
            this.Property(r => r.Topic).HasColumnName("Topic");
            this.Property(r => r.Welcome).HasColumnName("Welcome");

            // Relationships
            this.HasMany(r => r.AllowedUsers)
                .WithMany(u => u.AllowedRooms)
                .Map(m =>
                    {
                        m.ToTable("ChatRoomChatUser1");
                        m.MapLeftKey("ChatRoom_Key");
                        m.MapRightKey("ChatUser_Key");
                    });

            this.HasMany(r => r.Owners)
                .WithMany(u => u.OwnedRooms)
                .Map(m =>
                    {
                        m.ToTable("ChatRoomChatUsers");
                        m.MapLeftKey("ChatRoom_Key");
                        m.MapRightKey("ChatUser_Key");
                    });

            this.HasMany(r => r.Users)
                .WithMany(u => u.Rooms)
                .Map(m =>
                    {
                        m.ToTable("ChatUserChatRooms");
                        m.MapLeftKey("ChatRoom_Key");
                        m.MapRightKey("ChatUser_Key");
                    });

            this.HasOptional(r => r.Creator)
                .WithMany()
                .HasForeignKey(r => r.CreatorKey);

        }
    }
}