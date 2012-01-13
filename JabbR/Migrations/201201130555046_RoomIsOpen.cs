namespace JabbR.Models.Migrations
{
    using System.Data.Entity.Migrations;
    
    public partial class RoomIsOpen : DbMigration
    {
        public override void Up()
        {
            AddColumn("ChatRooms", "IsOpen", c => c.Boolean(nullable: false, defaultValue:true));
        }
        
        public override void Down()
        {
            DropColumn("ChatRooms", "IsOpen");
        }
    }
}
