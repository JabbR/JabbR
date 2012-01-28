using System;
using System.Data.Entity.Migrations;
using System.IO;

namespace JabbR.Models.Migrations
{
    public partial class ElmahInSql : DbMigration
    {
        private static readonly string[] Go = new[] { "GO" };

        public override void Up()
        {
            using (var stream = typeof(ElmahInSql).Assembly.GetManifestResourceStream("JabbR.Elmah.Elmah.SqlServer.sql"))
            {
                using (var streamReader = new StreamReader(stream))
                {
                    var statements = streamReader.ReadToEnd().Split(Go, StringSplitOptions.RemoveEmptyEntries);

                    foreach (var statement in statements)
                    {
                        if (String.IsNullOrWhiteSpace(statement))
                        {
                            continue;
                        }

                        Sql(statement);
                    }
                }
            }
        }

        public override void Down()
        {
            // No idea
        }
    }
}
