namespace JabbR.Models.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class MultipleIdentities : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.ChatUserIdentities",
                c => new
                    {
                        Key = c.Int(nullable: false, identity: true),
                        UserKey = c.Int(nullable: false),
                        Email = c.String(),
                        Identity = c.String(),
                    })
                .PrimaryKey(t => t.Key)
                .ForeignKey("dbo.ChatUsers", t => t.UserKey, cascadeDelete: true)
                .Index(t => t.UserKey);
            
        }
        
        public override void Down()
        {
            DropIndex("dbo.ChatUserIdentities", new[] { "UserKey" });
            DropForeignKey("dbo.ChatUserIdentities", "UserKey", "dbo.ChatUsers");
            DropTable("dbo.ChatUserIdentities");
        }
    }
}
