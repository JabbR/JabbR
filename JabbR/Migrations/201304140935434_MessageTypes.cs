namespace JabbR.Models.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class MessageTypes : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.ChatMessages", "MessageType", c => c.Int(nullable: false, defaultValue: 0));
        }
        
        public override void Down()
        {
            DropColumn("dbo.ChatMessages", "MessageType");
        }
    }
}
