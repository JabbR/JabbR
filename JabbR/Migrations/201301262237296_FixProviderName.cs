namespace JabbR.Models.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class FixProviderName : DbMigration
    {
        public override void Up()
        {
            RenameColumn("dbo.ChatUserIdentities", "ProvierName", "ProviderName");
        }
        
        public override void Down()
        {
            RenameColumn("dbo.ChatUserIdentities", "ProviderName", "ProvierName");
        }
    }
}
