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
    internal class PostgresReady
    {

        internal static bool ForChakraSync()
        {
            if (!PostgresDbPicker.SelectFromLocalStorage())
            {
                if (!PostgresDbPicker.GetFromUser())
                {
                    return false;
                }
            }

            string? response;

            if (AppSession.PostgresDatabase != null &&
                AppSession.PostgresDatabase.DatabasePrepped)
            {
                Console.ForegroundColor = ConsoleColor.DarkGreen;
                Console.WriteLine("This Postgres database has already been prepped for redfly.");
                Console.WriteLine("Do you want to walkthrough the prep instructions again? (y/n)");
                response = Console.ReadLine();
                Console.ResetColor();

                if (response != null &&
                    response.Equals("n", StringComparison.CurrentCultureIgnoreCase))
                {
                    return true;
                }
            }

            do
            {
                Console.WriteLine("Please ensure the following configuration is enabled in your PostgreSQL configuration (postgresql.conf):");
                Console.WriteLine("This is usually done by an Administrator");
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("    wal_level = logical\r\n");
                Console.ResetColor();

                Console.WriteLine("Please enter 'y' when you are ready to continue AFTER modifying this setting if it was different...");
                response = Console.ReadLine();
            }
            while (response == null ||
                   !response.Equals("y", StringComparison.CurrentCultureIgnoreCase));

            do
            {
                Console.WriteLine("These should be the MIN values for these configuration entries:");
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("    max_replication_slots >= 4");
                Console.WriteLine("    max_wal_senders >= 4\r\n");
                Console.ResetColor();

                Console.WriteLine("Please enter 'y' when you are ready to continue AFTER modifying these settings if they were different...");
                response = Console.ReadLine();
            }
            while (response == null ||
                   !response.Equals("y", StringComparison.CurrentCultureIgnoreCase));

            do
            {
                Console.WriteLine("You will also need to grant the REPLICATION privilege to the database user.");
                Console.WriteLine("This can be done using the following script:");
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("ALTER ROLE your_username WITH REPLICATION;\r\n");
                Console.ResetColor();

                Console.WriteLine("Please enter 'y' when you are ready to continue AFTER granting the privilege if it was not already granted...");
                response = Console.ReadLine();
            }
            while (response == null ||
                   !response.Equals("y", StringComparison.CurrentCultureIgnoreCase));

            do
            {
                Console.WriteLine("If you modified ANY of the following parameters, you will need to restart your Postgres server for them to take effect");
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("wal_level, max_replication_slots, max_wal_senders\r\n");
                Console.ResetColor();

                Console.WriteLine("Please enter 'y' when you are ready to continue AFTER the restart was done if found necessary...");
                response = Console.ReadLine();
            }
            while (response == null ||
                   !response.Equals("y", StringComparison.CurrentCultureIgnoreCase));

            if (AppSession.PostgresDatabase != null)
            {
                var collection = new LitePostgresDatabaseCollection();

                var row = collection.Find(AppSession.PostgresDatabase.EncryptedServerName,
                                          AppSession.PostgresDatabase.EncryptedDatabaseName,
                                          AppSession.PostgresDatabase.EncryptedUserName);

                row.DatabasePrepped = true;
                collection.Update(row);

                AppSession.PostgresDatabase = row;
            }

            return true;
        }

    }
}
