namespace Chat.Models.Migrations
{
    using System.Data.Entity.Migrations;
    
    public partial class RoomOwners : DbMigration
    {
        public override void Up()
        {
            AddColumn("ChatRooms", "ChatUser_Key", c => c.Int());
            AddColumn("ChatRooms", "Owner_Key", c => c.Int());
            AddColumn("ChatUsers", "ChatRoom_Key", c => c.Int());
            AddForeignKey("ChatRooms", "ChatUser_Key", "ChatUsers", "Key");
            AddForeignKey("ChatRooms", "Owner_Key", "ChatUsers", "Key");
            AddForeignKey("ChatUsers", "ChatRoom_Key", "ChatRooms", "Key");
        }
        
        public override void Down()
        {            
            DropForeignKey("ChatUsers", "ChatRoom_Key", "ChatRooms", "Key");
            DropForeignKey("ChatRooms", "Owner_Key", "ChatUsers", "Key");
            DropForeignKey("ChatRooms", "ChatUser_Key", "ChatUsers", "Key");
            DropColumn("ChatUsers", "ChatRoom_Key");
            DropColumn("ChatRooms", "Owner_Key");
            DropColumn("ChatRooms", "ChatUser_Key");
        }
    }
}
