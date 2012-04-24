namespace JabbR.Models.Migrations
{
    using System.Data.Entity.Migrations;
    
    public partial class MaxNoteLength : DbMigration
    {
        public override void Up()
        {
            AlterColumn("ChatUsers", "Note", c => c.String(maxLength: 200));
        }
        
        public override void Down()
        {
            AlterColumn("ChatUsers", "Note", c => c.String());
        }
    }
}
