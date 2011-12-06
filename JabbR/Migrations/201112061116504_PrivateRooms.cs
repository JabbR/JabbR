namespace JabbR.Models.Migrations
{
    using System.Data.Entity.Migrations;
    
    public partial class PrivateRooms : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "ChatRoomChatUser1",
                c => new
                    {
                        ChatRoom_Key = c.Int(nullable: false),
                        ChatUser_Key = c.Int(nullable: false),
                    })
                .PrimaryKey(t => new { t.ChatRoom_Key, t.ChatUser_Key })
                .ForeignKey("ChatRooms", t => t.ChatRoom_Key)
                .ForeignKey("ChatUsers", t => t.ChatUser_Key);
            
            AddColumn("ChatRooms", "Private", c => c.Boolean(nullable: false));
        }
        
        public override void Down()
        {
            DropForeignKey("ChatRoomChatUser1", "ChatUser_Key", "ChatUsers", "Key");
            DropForeignKey("ChatRoomChatUser1", "ChatRoom_Key", "ChatRooms", "Key");
            DropColumn("ChatRooms", "Private");
            DropTable("ChatRoomChatUser1");
        }
    }
}
