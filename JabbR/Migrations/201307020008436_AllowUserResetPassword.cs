namespace JabbR.Models.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AllowUserResetPassword : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.ChatUsers", "RequestPasswordResetId", c => c.String());
            AddColumn("dbo.ChatUsers", "RequestPasswordResetValidThrough", c => c.DateTimeOffset());
        }
        
        public override void Down()
        {
            DropColumn("dbo.ChatUsers", "RequestPasswordResetValidThrough");
            DropColumn("dbo.ChatUsers", "RequestPasswordResetId");
        }
    }
}
