namespace JabbR.Models.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class ExtendChatUserFlagLength : DbMigration
    {
        public override void Up()
        {
            AlterColumn("dbo.ChatUsers", "Flag", c => c.String(maxLength: 255));
        }
        
        public override void Down()
        {
            AlterColumn("dbo.ChatUsers", "Flag", c => c.String(maxLength: 2));
        }
    }
}
