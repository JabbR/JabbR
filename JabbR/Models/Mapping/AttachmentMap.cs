using System.Data.Entity.ModelConfiguration;

namespace JabbR.Models.Mapping
{
    public class AttachmentMap : EntityTypeConfiguration<Attachment>
    {
        public AttachmentMap()
        {
            // Primary Key
            this.HasKey(m => m.Key);

            // Properties
            // Table & Column Mappings
            this.ToTable("Attachments");
            this.Property(m => m.Key).HasColumnName("Key");
            this.Property(m => m.Id).HasColumnName("Id");
            this.Property(m => m.Url).HasColumnName("Url");
            this.Property(m => m.When).HasColumnName("When");
            this.Property(m => m.FileName).HasColumnName("FileName");
            this.Property(m => m.ContentType).HasColumnName("ContentType");
            this.Property(m => m.Size).HasColumnName("Size");

            this.HasRequired(a => a.Room)
                .WithMany(r => r.Attachments)
                .HasForeignKey(a => a.RoomKey);

            this.HasRequired(a => a.Owner)
                .WithMany(r => r.Attachments)
                .HasForeignKey(a => a.OwnerKey);
        }
    }
}