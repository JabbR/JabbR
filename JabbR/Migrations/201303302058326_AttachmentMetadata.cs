namespace JabbR.Models.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AttachmentMetadata : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Attachments", "FileName", c => c.String());
            AddColumn("dbo.Attachments", "ContentType", c => c.String());
            AddColumn("dbo.Attachments", "Size", c => c.Long(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.Attachments", "Size");
            DropColumn("dbo.Attachments", "ContentType");
            DropColumn("dbo.Attachments", "FileName");
        }
    }
}
