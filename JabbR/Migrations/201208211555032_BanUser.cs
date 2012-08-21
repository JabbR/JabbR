namespace JabbR.Models.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class BanUser : DbMigration
    {
        public override void Up()
        {
            AddColumn("ChatUsers", "IsBanned", c => c.Boolean(nullable: false, defaultValue: false));
        }
        
        public override void Down()
        {
            DropColumn("ChatUsers", "IsBanned");
        }
    }
}
