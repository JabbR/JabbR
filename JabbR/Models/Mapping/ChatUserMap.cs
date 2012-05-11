using System.ComponentModel.DataAnnotations;
using System.Data.Entity.ModelConfiguration;

namespace JabbR.Models.Mapping
{
    public class ChatUserMap : EntityTypeConfiguration<ChatUser>
    {
        public ChatUserMap()
        {
            // Primary Key
            this.HasKey(u => u.Key);

            // Properties
            this.Property(u => u.Note)
                .HasMaxLength(200);

            this.Property(u => u.AfkNote)
                .HasMaxLength(200);

            this.Property(u => u.Flag)
                .HasMaxLength(2);

            // Table & Column Mappings
            this.ToTable("ChatUsers");
            this.Property(u => u.Key).HasColumnName("Key");
            this.Property(u => u.Id).HasColumnName("Id");
            this.Property(u => u.Name).HasColumnName("Name");
            this.Property(u => u.Hash).HasColumnName("Hash");
            this.Property(u => u.LastActivity).HasColumnName("LastActivity");
            this.Property(u => u.LastNudged).HasColumnName("LastNudged");
            this.Property(u => u.Status).HasColumnName("Status");
            this.Property(u => u.HashedPassword).HasColumnName("HashedPassword");
            this.Property(u => u.Salt).HasColumnName("Salt");
            this.Property(u => u.Note).HasColumnName("Note");
            this.Property(u => u.AfkNote).HasColumnName("AfkNote");
            this.Property(u => u.IsAfk).HasColumnName("IsAfk");
            this.Property(u => u.Flag).HasColumnName("Flag");
            this.Property(u => u.Identity).HasColumnName("Identity");
            this.Property(u => u.Email).HasColumnName("Email");
            this.Property(u => u.IsAdmin).HasColumnName("IsAdmin");
        }
    }
}