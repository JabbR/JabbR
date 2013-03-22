namespace JabbR.Models.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class ContentProviderContent : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.ChatMessages", "HtmlContent", c => c.String());
        }
        
        public override void Down()
        {
            DropColumn("dbo.ChatMessages", "HtmlContent");
        }
    }
}
