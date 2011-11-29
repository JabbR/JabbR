namespace JabbR.Models.Migrations
{
    using System.Data.Entity.Migrations;
    
    public partial class Passwords : DbMigration
    {
        public override void Up()
        {
            AddColumn("ChatUsers", "HashedPassword", c => c.String());
        }
        
        public override void Down()
        {
            DropColumn("ChatUsers", "HashedPassword");
        }
    }
}
