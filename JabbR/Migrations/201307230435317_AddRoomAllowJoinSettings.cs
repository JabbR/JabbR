namespace JabbR.Models.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddRoomAllowJoinSettings : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.ChatRooms", "OwnersCanAllow", c => c.Boolean(nullable: false));
            AddColumn("dbo.ChatRooms", "UsersCanAllow", c => c.Boolean(nullable: false));
            Sql("UPDATE dbo.ChatRooms SET OwnersCanAllow = '1'");
            Sql("UPDATE dbo.ChatRooms SET UsersCanAllow = '1'");
        }
        
        public override void Down()
        {
            DropColumn("dbo.ChatRooms", "UsersCanAllow");
            DropColumn("dbo.ChatRooms", "OwnersCanAllow");
        }
    }
}
