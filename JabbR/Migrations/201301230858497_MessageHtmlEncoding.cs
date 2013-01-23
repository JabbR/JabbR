namespace JabbR.Models.Migrations
{
    using System;
    using System.Data.Entity.Migrations;

    public partial class MessageHtmlEncoding : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.ChatMessages", "HtmlEncoded", c => c.Boolean(nullable: false, defaultValue: true));
        }

        public override void Down()
        {
            DropColumn("dbo.ChatMessages", "HtmlEncoded");
        }
    }
}
