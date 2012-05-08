namespace JabbR.Models.Migrations
{
    using System.Data.Entity.Migrations;
    
    public partial class AdminUser : DbMigration
    {
        public override void Up()
        {
            AddColumn("ChatUsers", "IsAdmin", c => c.Boolean(nullable: false, defaultValue: false));
        }
        
        public override void Down()
        {
            DropColumn("ChatUsers", "IsAdmin");
        }
    }
}
