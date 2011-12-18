namespace JabbR.Models.Migrations
{
    using System.Data.Entity.Migrations;
    
    public partial class UserNote : DbMigration
    {
        public override void Up()
        {
            AddColumn("ChatUsers", "Note", c => c.String());
        }
        
        public override void Down()
        {
            DropColumn("ChatUsers", "Note");
        }
    }
}
