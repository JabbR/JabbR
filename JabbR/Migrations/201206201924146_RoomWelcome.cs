namespace JabbR.Models.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class RoomWelcome : DbMigration
    {
        public override void Up()
        {
            AddColumn("ChatRooms", "Welcome", c => c.String(maxLength: 200));
        }

        public override void Down()
        {
            DropColumn("ChatRooms", "Welcome");
        }
    }
}
