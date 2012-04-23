namespace JabbR.Models.Migrations
{
    using System.Data.Entity.Migrations;
    
    public partial class Indexes : DbMigration
    {
        public override void Up()
        {
            ChangeColumn("ChatUsers", "Id", c => c.String(maxLength: 200));
            ChangeColumn("ChatRooms", "Name", c => c.String(maxLength: 200));
            CreateIndex("ChatUsers", "Id", unique: true);
            CreateIndex("ChatRooms", "Name", unique: true);
        }
        
        public override void Down()
        {
            DropIndex("ChatRooms", "Name");
            DropIndex("ChatUsers", "Id");
            ChangeColumn("ChatUsers", "Id", c => c.String());
            ChangeColumn("ChatRooms", "Name", c => c.String());
        }
    }
}
