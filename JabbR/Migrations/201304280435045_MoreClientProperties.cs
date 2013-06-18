namespace JabbR.Models.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class MoreClientProperties : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.ChatClients", "Name", c => c.String());
            AddColumn("dbo.ChatClients", "LastClientActivity", c => c.DateTimeOffset(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.ChatClients", "LastClientActivity");
            DropColumn("dbo.ChatClients", "Name");
        }
    }
}
