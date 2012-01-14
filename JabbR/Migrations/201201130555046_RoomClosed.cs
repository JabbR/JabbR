namespace JabbR.Models.Migrations
{
    using System.Data.Entity.Migrations;
    
    public partial class RoomIsOpen : DbMigration
    {
        public override void Up()
        {
            AddColumn("ChatRooms", "Closed", c => c.Boolean(nullable: false, defaultValue:false));
        }
        
        public override void Down()
        {
            DropColumn("ChatRooms", "Closed");
        }
    }
}
