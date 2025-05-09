using LiteDB;
using Microsoft.Data.SqlClient;
using RedflyCoreFramework;
using RedflyLocalStorage;
using RedflyLocalStorage.Collections;
using RedflyLocalStorage.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace RedflyDatabaseSyncProxy
{
    internal class MongoReady
    {

        internal static bool ForChakraSync()
        {
            if (!MongoDbPicker.SelectFromLocalStorage())
            {
                if (!MongoDbPicker.GetFromUser())
                {
                    return false;
                }
            }

            string? response;

            if (AppSession.MongoDatabase != null &&
                AppSession.MongoDatabase.DatabasePrepped)
            {
                Console.ForegroundColor = ConsoleColor.DarkGreen;
                Console.WriteLine("This Mongo database has already been prepped for redfly.");
                Console.WriteLine("Do you want to walkthrough the prep instructions again? (y/n)");
                response = Console.ReadLine();
                Console.ResetColor();

                if (response != null &&
                    response.Equals("n", StringComparison.CurrentCultureIgnoreCase))
                {
                    return true;
                }
            }

            Console.WriteLine("Please ensure the following setup for your MongoDB Instance.");
            Console.WriteLine("These are necessary for MongoDB Change Streams to work properly.\r\n");

            do
            {
                Console.WriteLine("1. Deploy a Replica Set");
                Console.WriteLine("   MongoDB should be running as a replica set or in a sharded cluster");
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("   Stop the MongoDB server.");
                Console.WriteLine("   Start the MongoDB server with the --replSet option: mongod --replSet \"rs0\"");
                Console.WriteLine("   Initiate the replica set:");
                Console.WriteLine("   mongo");
                Console.WriteLine("   rs.initiate()");
                Console.ResetColor();                
                Console.WriteLine("   If you're using MongoDB Atlas, all clusters are deployed as replica sets by default\r\n");

                Console.WriteLine("Please enter 'y' when you are ready to continue AFTER ensuring this setup...");
                response = Console.ReadLine();
            }
            while (response == null ||
                   !response.Equals("y", StringComparison.CurrentCultureIgnoreCase));

            do
            {
                Console.WriteLine("2. Set Database User Permissions");
                Console.WriteLine("   Assign the readWrite role to the user for the database you want to monitor.");
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("   db.createUser({");
                Console.WriteLine("       user: \"changeStreamUser\",");
                Console.WriteLine("       pwd: \"password\",");
                Console.WriteLine("       roles: [");
                Console.WriteLine("           { role: \"read\", db: \"exampleDatabase\" },");
                Console.WriteLine("           { role: \"read\", db: \"local\" }");
                Console.WriteLine("       ]");
                Console.WriteLine("   });");
                Console.ResetColor();
                Console.WriteLine("   For monitoring all collections, you can also assign the read or readWrite role for the entire database.\r\n");

                Console.WriteLine("Please enter 'y' when you are ready to continue AFTER modifying the user role...");
                response = Console.ReadLine();
            }
            while (response == null ||
                   !response.Equals("y", StringComparison.CurrentCultureIgnoreCase));

            do
            {
                Console.WriteLine("3. Enable Change Streams on the Database");
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("   Change streams are supported starting from MongoDB 3.6.");
                Console.WriteLine("   Change streams are enabled by default for replica sets and sharded clusters.");
                Console.ResetColor();                
                Console.WriteLine("   Change Streams are enabled by default on MongoDB Atlas.");
                Console.WriteLine("   If you're using MongoDB Atlas, the database should not be using a Free Tier Cluster (M0).");
                Console.WriteLine("MongoDB Change Streams require at least an M10 or higher cluster.\r\n");

                Console.WriteLine("Please enter 'y' when you are ready to continue AFTER enabling change streams on the database...");
                response = Console.ReadLine();
            }
            while (response == null ||
                   !response.Equals("y", StringComparison.CurrentCultureIgnoreCase));

            do
            {
                Console.WriteLine("4. Increase Oplog Size (If Necessary)");
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("   Increase the oplog size to ensure that the oplog doesn't roll over before your application processes changes.");
                Console.ResetColor();
                Console.WriteLine("   Change Streams rely on the oplog (operations log).");
                Console.WriteLine("   This is necessary IF your database has a high write throughput.\r\n");

                Console.WriteLine("Please enter 'y' when you are ready to continue AFTER increasing the Oplog size (if found necessary)...");
                response = Console.ReadLine();
            }
            while (response == null ||
                   !response.Equals("y", StringComparison.CurrentCultureIgnoreCase));

            if (AppSession.MongoDatabase != null)
            {
                var collection = new LiteMongoDatabaseCollection();

                var row = collection.Find(AppSession.MongoDatabase.EncryptedServerName,
                                          AppSession.MongoDatabase.EncryptedDatabaseName,
                                          AppSession.MongoDatabase.EncryptedUserName);

                row.DatabasePrepped = true;
                collection.Update(row);

                AppSession.MongoDatabase = row;
            }

            return true;
        }

    }
}
