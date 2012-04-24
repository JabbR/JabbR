namespace JabbR.Models.Migrations
{
    using System.Data.Entity.Migrations;
    
    public partial class UserSalt : DbMigration
    {
        public override void Up()
        {
            AddColumn("ChatUsers", "Salt", c => c.String());
            AlterColumn("ChatClients", "User_Key", c => c.Int(nullable: false));
        }
        
        public override void Down()
        {
            AlterColumn("ChatClients", "User_Key", c => c.Int());
            DropColumn("ChatUsers", "Salt");
        }
    }
}
