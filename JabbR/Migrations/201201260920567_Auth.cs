namespace JabbR.Models.Migrations
{
    using System.Data.Entity.Migrations;
    
    public partial class Auth : DbMigration
    {
        public override void Up()
        {
            AddColumn("ChatUsers", "Identity", c => c.String());
            AddColumn("ChatUsers", "Email", c => c.String());
        }
        
        public override void Down()
        {
            DropColumn("ChatUsers", "Email");
            DropColumn("ChatUsers", "Identity");
        }
    }
}
