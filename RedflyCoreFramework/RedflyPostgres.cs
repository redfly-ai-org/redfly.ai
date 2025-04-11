using Npgsql;
using System;

namespace RedflyCoreFramework
{
    public class RedflyPostgres
    {
        public static bool VerifyConnectivity(string? serverName, string? databaseName, string? userName, string password)
        {
            // Return false if any of the parameters are null
            if (serverName == null || databaseName == null || userName == null)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("One or more of the required parameters are null.");
                Console.ResetColor();
                return false;
            }

            var connectionString = $"Host={serverName};Database={databaseName};Username={userName};Password={password};";

            try
            {
                using (var connection = new NpgsqlConnection(connectionString))
                {
                    connection.Open();
                    Console.WriteLine("Successfully connected to the Postgres database.");
                    return true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to connect to the Postgres database: {ex.Message}");
                return false;
            }
        }
    }
}
