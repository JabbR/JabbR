namespace JabbR.Models.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddUserPreferences : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.ChatUserPreferences",
                c => new
                    {
                        ChatUserId = c.Int(nullable: false),
                        RoomId = c.Int(nullable: false),
                        Key = c.String(nullable: false, maxLength: 128),
                        Value = c.String(),
                    })
                .PrimaryKey(t => new { t.ChatUserId, t.RoomId, t.Key })
                .ForeignKey("dbo.ChatUsers", t => t.ChatUserId, cascadeDelete: true)
                .ForeignKey("dbo.ChatRooms", t => t.RoomId, cascadeDelete: true)
                .Index(t => t.ChatUserId)
                .Index(t => t.RoomId);
            
        }
        
        public override void Down()
        {
            DropIndex("dbo.ChatUserPreferences", new[] { "RoomId" });
            DropIndex("dbo.ChatUserPreferences", new[] { "ChatUserId" });
            DropForeignKey("dbo.ChatUserPreferences", "RoomId", "dbo.ChatRooms");
            DropForeignKey("dbo.ChatUserPreferences", "ChatUserId", "dbo.ChatUsers");
            DropTable("dbo.ChatUserPreferences");
        }
    }
}
