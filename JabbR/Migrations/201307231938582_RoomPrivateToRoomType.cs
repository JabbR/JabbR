namespace JabbR.Models.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class RoomPrivateToRoomType : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.ChatRooms", "RoomType", c => c.Int(nullable: false));
            Sql("UPDATE dbo.ChatRooms SET RoomType = Private");
            DropColumn("dbo.ChatRooms", "Private");
        }
        
        public override void Down()
        {
            AddColumn("dbo.ChatRooms", "Private", c => c.Boolean(nullable: false));
            Sql("UPDATE dbo.ChatRooms SET Private = CASE WHEN RoomType = '0' THEN '0' ELSE '1' END");
            DropColumn("dbo.ChatRooms", "RoomType");
        }
    }
}
