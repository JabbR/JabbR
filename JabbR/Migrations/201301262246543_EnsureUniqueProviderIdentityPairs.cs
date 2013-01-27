namespace JabbR.Models.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class EnsureUniqueProviderIdentityPairs : DbMigration
    {
        public override void Up()
        {
            CreateIndex(table: "dbo.ChatUserIdentities", columns: new[] { "Identity", "ProviderName" }, unique: true,
                        name: "idx_uq_provider_identity");
        }
        
        public override void Down()
        {
            DropIndex("dbo.ChatUserIdentities", name: "idx_uq_provider_identity");
        }
    }
}
