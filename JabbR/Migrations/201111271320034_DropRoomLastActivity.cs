namespace JabbR.Models.Migrations
{
    using System.Data.Entity.Migrations;
    
    public partial class DropRoomLastActivity : DbMigration
    {
        public override void Up()
        {
            DropColumn("ChatRooms", "LastActivity");
        }
        
        public override void Down()
        {
            AddColumn("ChatRooms", "LastActivity", c => c.DateTime(nullable: false));
        }
    }
}
