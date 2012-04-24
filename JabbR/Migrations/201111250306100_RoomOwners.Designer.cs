namespace JabbR.Models.Migrations
{
    using System.Data.Entity.Migrations.Infrastructure;
    
    public partial class RoomOwners : IMigrationMetadata
    {
        string IMigrationMetadata.Id
        {
            get { return "201111250306100_RoomOwners"; }
        }
        
        string IMigrationMetadata.Source
        {
            get { return null; }
        }
        
        string IMigrationMetadata.Target
        {
            get { return "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"; }
        }
    }
}
