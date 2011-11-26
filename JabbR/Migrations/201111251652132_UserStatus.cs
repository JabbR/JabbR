namespace JabbR.Models.Migrations
{
    using System.Data.Entity.Migrations;
    
    public partial class UserStatus : DbMigration
    {
        public override void Up()
        {
            AddColumn("ChatUsers", "Status", c => c.Int(nullable: false));
            DropColumn("ChatUsers", "Active");
        }
        
        public override void Down()
        {
            AddColumn("ChatUsers", "Active", c => c.Boolean(nullable: false));
            DropColumn("ChatUsers", "Status");
        }
    }
}
