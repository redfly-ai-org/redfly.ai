using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedflyCoreFramework
{
    public class RedflySqlServer
    {

        public static bool VerifyConnectivity(string? serverName, string? databaseName, string? userName, string password)
        {
            //Return false if any of the parameters are null
            if (serverName == null || databaseName == null || userName == null)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("One or more of the required parameters are null.");
                Console.ResetColor();
                return false;
            }

            var connectionString = $"Server={serverName};Database={databaseName};User Id={userName};Password={password};";

            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    Console.WriteLine("Connecting to SQL Server database...");
                    connection.Open();
                    Console.WriteLine("Successfully connected to the Sql Server database.");
                    return true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to connect to the Sql Server database: {ex.Message}");
                return false;
            }
        }

    }
}
