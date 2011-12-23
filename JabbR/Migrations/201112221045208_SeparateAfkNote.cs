namespace JabbR.Models.Migrations
{
    using System.Data.Entity.Migrations;
    
    public partial class SeparateAfkNote : DbMigration
    {
        public override void Up()
        {
            AddColumn("ChatUsers", "AfkNote", c => c.String(maxLength: 200));
            AddColumn("ChatUsers", "IsAfk", c => c.Boolean(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("ChatUsers", "IsAfk");
            DropColumn("ChatUsers", "AfkNote");
        }
    }
}
