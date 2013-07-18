namespace JabbR.Models.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddUserPreferences : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.ChatUsers", "RawPreferences", c => c.String());
        }
        
        public override void Down()
        {
            DropColumn("dbo.ChatUsers", "RawPreferences");
        }
    }
}
