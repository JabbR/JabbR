using System.Data.Entity.ModelConfiguration;

namespace JabbR.Models.Mapping
{
    public class ChatUserIdentityMap : EntityTypeConfiguration<ChatUserIdentity>
    {
        public ChatUserIdentityMap()
        {
            // Primary Key
            this.HasKey(c => c.Key);

            // Properties
            // Table & Column Mappings
            this.ToTable("ChatUserIdentities");
            this.Property(c => c.Key).HasColumnName("Key");
            this.Property(c => c.Identity).HasColumnName("Identity");
            this.Property(c => c.UserKey).HasColumnName("User_Key");
            this.Property(c => c.Email).HasColumnName("Email");
            this.Property(c => c.ProviderName).HasColumnName("ProviderName");

            // Relationships
            this.HasRequired(c => c.User)
                .WithMany(u => u.Identities)
                .HasForeignKey(c => c.UserKey);

        }
    }
}