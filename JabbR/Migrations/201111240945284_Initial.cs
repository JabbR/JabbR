namespace JabbR.Models.Migrations
{
    using System.Data.Entity.Migrations;
    
    public partial class Initial : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "ChatRooms",
                c => new
                    {
                        Key = c.Int(nullable: false, identity: true),
                        LastActivity = c.DateTime(nullable: false),
                        LastNudged = c.DateTime(),
                        Name = c.String(),
                    })
                .PrimaryKey(t => t.Key);
            
            CreateTable(
                "ChatMessages",
                c => new
                    {
                        Key = c.Int(nullable: false, identity: true),
                        Content = c.String(),
                        Id = c.String(),
                        When = c.DateTimeOffset(nullable: false),
                        Room_Key = c.Int(),
                        User_Key = c.Int(),
                    })
                .PrimaryKey(t => t.Key)
                .ForeignKey("ChatRooms", t => t.Room_Key)
                .ForeignKey("ChatUsers", t => t.User_Key);
            
            CreateTable(
                "ChatUsers",
                c => new
                    {
                        Key = c.Int(nullable: false, identity: true),
                        Id = c.String(),
                        Name = c.String(),
                        Hash = c.String(),
                        Active = c.Boolean(nullable: false),
                        LastActivity = c.DateTime(nullable: false),
                        LastNudged = c.DateTime(),
                        ClientId = c.String(),
                    })
                .PrimaryKey(t => t.Key);
            
            CreateTable(
                "EdmMetadata",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        ModelHash = c.String(),
                    })
                .PrimaryKey(t => t.Id);
            
            CreateTable(
                "ChatUserChatRooms",
                c => new
                    {
                        ChatUser_Key = c.Int(nullable: false),
                        ChatRoom_Key = c.Int(nullable: false),
                    })
                .PrimaryKey(t => new { t.ChatUser_Key, t.ChatRoom_Key })
                .ForeignKey("ChatUsers", t => t.ChatUser_Key)
                .ForeignKey("ChatRooms", t => t.ChatRoom_Key);
            
        }
        
        public override void Down()
        {
            DropForeignKey("ChatUserChatRooms", "ChatRoom_Key", "ChatRooms", "Key");
            DropForeignKey("ChatUserChatRooms", "ChatUser_Key", "ChatUsers", "Key");
            DropForeignKey("ChatMessages", "User_Key", "ChatUsers", "Key");
            DropForeignKey("ChatMessages", "Room_Key", "ChatRooms", "Key");
            DropTable("ChatUserChatRooms");
            DropTable("EdmMetadata");
            DropTable("ChatUsers");
            DropTable("ChatMessages");
            DropTable("ChatRooms");
        }
    }
}
