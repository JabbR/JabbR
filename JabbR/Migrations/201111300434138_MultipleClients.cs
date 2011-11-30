namespace JabbR.Models.Migrations
{
    using System.Data.Entity.Migrations;
    
    public partial class MultipleClients : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "ChatClients",
                c => new
                    {
                        Key = c.Int(nullable: false, identity: true),
                        Id = c.String(),
                        User_Key = c.Int(),
                    })
                .PrimaryKey(t => t.Key)
                .ForeignKey("ChatUsers", t => t.User_Key);
            
            DropColumn("ChatUsers", "ClientId");
        }
        
        public override void Down()
        {
            AddColumn("ChatUsers", "ClientId", c => c.String());
            DropForeignKey("ChatClients", "User_Key", "ChatUsers", "Key");
            DropTable("ChatClients");
        }
    }
}
