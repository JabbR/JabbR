using System;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using JabbR.Models.Mapping;

namespace JabbR.Models
{
    public class JabbrContext : DbContext
    {
        public JabbrContext()
            : base("Jabbr")
        {
            ((IObjectContextAdapter)this).ObjectContext.ObjectMaterialized += ObjectContext_ObjectMaterialized;
        }

        void ObjectContext_ObjectMaterialized(object sender, System.Data.Objects.ObjectMaterializedEventArgs e)
        {
            var entityChatUser = e.Entity as ChatUser;
            if (entityChatUser != null)
            {
                entityChatUser.LastActivity = DateTime.SpecifyKind(entityChatUser.LastActivity, DateTimeKind.Utc);
            }
        }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Configurations.Add(new ChatClientMap());

            modelBuilder.Configurations.Add(new ChatMessageMap());

            modelBuilder.Configurations.Add(new ChatRoomMap());

            modelBuilder.Configurations.Add(new ChatUserMap());

            modelBuilder.Configurations.Add(new AttachmentMap());

            modelBuilder.Configurations.Add(new NotificationMap());

            base.OnModelCreating(modelBuilder);
        }

        public DbSet<ChatClient> Clients { get; set; }
        public DbSet<ChatMessage> Messages { get; set; }
        public DbSet<ChatRoom> Rooms { get; set; }
        public DbSet<ChatUser> Users { get; set; }
        public DbSet<ChatUserIdentity> Identities { get; set; }
        public DbSet<Attachment> Attachments { get; set; }
        public DbSet<Notification> Notifications { get; set; }
    }
}