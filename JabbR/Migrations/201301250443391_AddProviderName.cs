namespace JabbR.Models.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddProviderName : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.ChatUserIdentities", "ProvierName", c => c.String());
        }
        
        public override void Down()
        {
            DropColumn("dbo.ChatUserIdentities", "ProvierName");
        }
    }
}
