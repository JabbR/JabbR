using System;
using System.Data.Entity;
using System.Data.Entity.Migrations;
using JabbR.Models;
using JabbR.Models.Migrations;
using JabbR.Services;

namespace JabbR
{
    public partial class Startup
    {
        private const string SqlClient = "System.Data.SqlClient";

        private static void DoMigrations(IJabbrConfiguration config)
        {
            if (String.IsNullOrEmpty(config.SqlConnectionString.ProviderName) ||
                !config.SqlConnectionString.ProviderName.Equals(SqlClient, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            Database.SetInitializer<JabbrContext>(null);

            // Only run migrations for SQL server (Sql ce not supported as yet)
            var settings = new MigrationsConfiguration();
            var migrator = new DbMigrator(settings);
            migrator.Update();
        }
    }
}