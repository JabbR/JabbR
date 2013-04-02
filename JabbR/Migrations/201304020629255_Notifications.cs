namespace JabbR.Models.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class Notifications : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.Notifications",
                c => new
                    {
                        Key = c.Int(nullable: false, identity: true),
                        UserKey = c.Int(nullable: false),
                        MessageKey = c.Int(nullable: false),
                        Read = c.Boolean(nullable: false),
                    })
                .PrimaryKey(t => t.Key)
                .ForeignKey("dbo.ChatUsers", t => t.UserKey, cascadeDelete: true)
                .ForeignKey("dbo.ChatMessages", t => t.MessageKey, cascadeDelete: true)
                .Index(t => t.UserKey)
                .Index(t => t.MessageKey);
            
        }
        
        public override void Down()
        {
            DropIndex("dbo.Notifications", new[] { "MessageKey" });
            DropIndex("dbo.Notifications", new[] { "UserKey" });
            DropForeignKey("dbo.Notifications", "MessageKey", "dbo.ChatMessages");
            DropForeignKey("dbo.Notifications", "UserKey", "dbo.ChatUsers");
            DropTable("dbo.Notifications");
        }
    }
}
