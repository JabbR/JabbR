namespace JabbR.Models.Migrations
{
    using System.Data.Entity.Migrations;
    
    public partial class MultipleOwners : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "ChatRoomChatUsers",
                c => new
                    {
                        ChatRoom_Key = c.Int(nullable: false),
                        ChatUser_Key = c.Int(nullable: false),
                    })
                .PrimaryKey(t => new { t.ChatRoom_Key, t.ChatUser_Key })
                .ForeignKey("ChatRooms", t => t.ChatRoom_Key)
                .ForeignKey("ChatUsers", t => t.ChatUser_Key);
            
            AddColumn("ChatRooms", "Creator_Key", c => c.Int());
            AddForeignKey("ChatRooms", "Creator_Key", "ChatUsers", "Key");
            DropForeignKey("ChatRooms", "Owner_Key", "ChatUsers", "Key");
            DropColumn("ChatRooms", "Owner_Key");
        }
        
        public override void Down()
        {
            AddColumn("ChatRooms", "Owner_Key", c => c.Int());
            AddForeignKey("ChatRooms", "Owner_Key", "ChatUsers", "Key");
            DropForeignKey("ChatRoomChatUsers", "ChatUser_Key", "ChatUsers", "Key");
            DropForeignKey("ChatRoomChatUsers", "ChatRoom_Key", "ChatRooms", "Key");
            DropForeignKey("ChatRooms", "Creator_Key", "ChatUsers", "Key");
            DropColumn("ChatRooms", "Creator_Key");
            DropTable("ChatRoomChatUsers");
        }
    }
}
