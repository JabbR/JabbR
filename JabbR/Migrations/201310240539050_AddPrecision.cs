namespace JabbR.Models.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddPrecision : DbMigration
    {
        public override void Up()
        {
            AlterColumn("dbo.ChatClients", "LastActivity", c => c.DateTimeOffset(nullable: false, precision: 7));
            AlterColumn("dbo.ChatClients", "LastClientActivity", c => c.DateTimeOffset(nullable: false, precision: 7));
            AlterColumn("dbo.ChatUsers", "RequestPasswordResetValidThrough", c => c.DateTimeOffset(precision: 7));
            AlterColumn("dbo.ChatMessages", "When", c => c.DateTimeOffset(nullable: false, precision: 7));
            AlterColumn("dbo.Attachments", "When", c => c.DateTimeOffset(nullable: false, precision: 7));
        }
        
        public override void Down()
        {
            AlterColumn("dbo.Attachments", "When", c => c.DateTimeOffset(nullable: false));
            AlterColumn("dbo.ChatMessages", "When", c => c.DateTimeOffset(nullable: false));
            AlterColumn("dbo.ChatUsers", "RequestPasswordResetValidThrough", c => c.DateTimeOffset());
            AlterColumn("dbo.ChatClients", "LastClientActivity", c => c.DateTimeOffset(nullable: false));
            AlterColumn("dbo.ChatClients", "LastActivity", c => c.DateTimeOffset(nullable: false));
        }
    }
}
