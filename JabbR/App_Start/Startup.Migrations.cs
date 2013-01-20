using System;
using System.Configuration;
using System.Data.Entity.Migrations;

namespace JabbR
{
    public partial class Startup
    {
        private const string SqlClient = "System.Data.SqlClient";

        private static void DoMigrations()
        {
            // Get the Jabbr connection string
            var connectionString = ConfigurationManager.ConnectionStrings["Jabbr"];

            if (String.IsNullOrEmpty(connectionString.ProviderName) ||
                !connectionString.ProviderName.Equals(SqlClient, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            // Only run migrations for SQL server (Sql ce not supported as yet)
            var settings = new JabbR.Models.Migrations.MigrationsConfiguration();
            var migrator = new DbMigrator(settings);
            migrator.Update();
        }
    }
}