namespace JabbR.Models.Migrations
{
    using System.Data.Entity.Migrations;
    
    public partial class UserFlag : DbMigration
    {
        public override void Up()
        {
            AddColumn("ChatUsers", "Flag", c => c.String(maxLength: 2));
        }
        
        public override void Down()
        {
            DropColumn("ChatUsers", "Flag");
        }
    }
}
