namespace JabbR.Models.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class RoomOwners : DbMigration
    {
        public override void Up()
        {
            AddColumn("ChatRooms", "Owner_Key", c => c.Int(nullable: true));
            AddForeignKey("ChatRooms", "Owner_Key", "ChatUsers", "Key");
        }

        public override void Down()
        {
            DropForeignKey("ChatRooms", "Owner_Key", "ChatUsers", "Key");
            DropColumn("ChatRooms", "Owner_Key");
        }
    }
}
