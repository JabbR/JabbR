namespace JabbR.Models.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class Attachments : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.Attachments",
                c => new
                    {
                        Key = c.Int(nullable: false, identity: true),
                        Url = c.String(),
                        Id = c.String(),
                        RoomKey = c.Int(nullable: false),
                        OwnerKey = c.Int(nullable: false),
                        When = c.DateTimeOffset(nullable: false),
                    })
                .PrimaryKey(t => t.Key)
                .ForeignKey("dbo.ChatRooms", t => t.RoomKey, cascadeDelete: true)
                .ForeignKey("dbo.ChatUsers", t => t.OwnerKey, cascadeDelete: true)
                .Index(t => t.RoomKey)
                .Index(t => t.OwnerKey);
            
        }
        
        public override void Down()
        {
            DropIndex("dbo.Attachments", new[] { "OwnerKey" });
            DropIndex("dbo.Attachments", new[] { "RoomKey" });
            DropForeignKey("dbo.Attachments", "OwnerKey", "dbo.ChatUsers");
            DropForeignKey("dbo.Attachments", "RoomKey", "dbo.ChatRooms");
            DropTable("dbo.Attachments");
        }
    }
}
