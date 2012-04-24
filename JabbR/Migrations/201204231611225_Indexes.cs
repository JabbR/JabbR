namespace JabbR.Models.Migrations
{
    using System.Data.Entity.Migrations;
    
    public partial class Indexes : DbMigration
    {
        public override void Up()
        {
            AlterColumn("ChatUsers", "Id", c => c.String(maxLength: 200));
            AlterColumn("ChatRooms", "Name", c => c.String(maxLength: 200));
            CreateIndex("ChatUsers", "Id", unique: true);
            CreateIndex("ChatRooms", "Name", unique: true);
        }
        
        public override void Down()
        {
            DropIndex("ChatRooms", "Name");
            DropIndex("ChatUsers", "Id");
            AlterColumn("ChatUsers", "Id", c => c.String());
            AlterColumn("ChatRooms", "Name", c => c.String());
        }
    }
}
