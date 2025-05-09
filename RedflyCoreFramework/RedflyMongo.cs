using MongoDB.Driver;
using System;

namespace RedflyCoreFramework
{
    public class RedflyMongo
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

            // Construct the MongoDB connection string
            var connectionString = $"mongodb+srv://{userName}:{password}@{serverName}/{databaseName}?retryWrites=true&w=majority";

            try
            {
                var client = new MongoClient(connectionString);

                Console.WriteLine("Connecting to MongoDB database...");
                var database = client.GetDatabase(databaseName);

                // Perform a simple operation to verify connectivity
                database.ListCollectionNames();
                Console.WriteLine("Successfully connected to the MongoDB database.");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to connect to the MongoDB database: {ex.Message}");
                return false;
            }
        }
    }
}
