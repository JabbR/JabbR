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
                        Key = c.Int(nullable: false),
                        UserKey = c.Int(nullable: false),
                        MessageKey = c.Int(nullable: false),
                        Read = c.Boolean(nullable: false),
                    })
                .PrimaryKey(t => t.Key)
                .ForeignKey("dbo.ChatUsers", t => t.UserKey, cascadeDelete: true)
                .ForeignKey("dbo.ChatMessages", t => t.Key)
                .Index(t => t.UserKey)
                .Index(t => t.Key);
            
        }
        
        public override void Down()
        {
            DropIndex("dbo.Notifications", new[] { "Key" });
            DropIndex("dbo.Notifications", new[] { "UserKey" });
            DropForeignKey("dbo.Notifications", "Key", "dbo.ChatMessages");
            DropForeignKey("dbo.Notifications", "UserKey", "dbo.ChatUsers");
            DropTable("dbo.Notifications");
        }
    }
}
