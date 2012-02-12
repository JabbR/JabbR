namespace JabbR.Models.Migrations
{
    using System.Data.Entity.Migrations;
    
    public partial class RoomTopic : DbMigration
    {
        public override void Up()
        {
            AddColumn("ChatRooms", "Topic", c => c.String(maxLength: 80));
        }
        
        public override void Down()
        {
            DropColumn("ChatRooms", "Topic");
        }
    }
}
