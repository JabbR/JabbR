namespace JabbR.Models.Migrations
{
    using System.Data.Entity.Migrations;
    
    public partial class InviteCodes : DbMigration
    {
        public override void Up()
        {
            AddColumn("ChatRooms", "InviteCode", c => c.String(nullable: true, maxLength: 6, fixedLength: true));
        }
        
        public override void Down()
        {
            DropColumn("ChatRooms", "InviteCode");
        }
    }
}
