namespace JabbR.Models.Migrations
{
    using System.Data.Entity.Migrations;
    
    public partial class ClientInfo : DbMigration
    {
        public override void Up()
        {
            AddColumn("ChatClients", "UserAgent", c => c.String());
            AddColumn("ChatClients", "LastActivity", c => c.DateTimeOffset(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("ChatClients", "LastActivity");
            DropColumn("ChatClients", "UserAgent");
        }
    }
}
