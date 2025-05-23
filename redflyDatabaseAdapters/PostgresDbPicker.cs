using LiteDB;
using RedflyCoreFramework;
using RedflyLocalStorage.Collections;
using RedflyLocalStorage;
using RedflyLocalStorage.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;

namespace redflyDatabaseAdapters
{
    public class PostgresDbPicker
    {

        public static bool GetFromUser()
        {
            string serverName = "";
            string databaseName = "";
            string userName = "";
            string password = "";
            string pgOutputSlotName = "";
            string publicationName = "";

            do
            {
                serverName = "";
                databaseName = "";
                userName = "";
                password = "";
                pgOutputSlotName = "";
                publicationName = "";

                while (string.IsNullOrWhiteSpace(serverName))
                {
                    Console.WriteLine("Please enter the Postgres server name:");
                    serverName = Console.ReadLine() ?? string.Empty;
                }

                while (string.IsNullOrWhiteSpace(databaseName))
                {
                    Console.WriteLine("Please enter the Postgres database name:");
                    databaseName = Console.ReadLine() ?? string.Empty;
                }

                while (string.IsNullOrWhiteSpace(userName))
                {
                    Console.WriteLine("Please enter the Postgres username:");
                    userName = Console.ReadLine() ?? string.Empty;
                }

                while (string.IsNullOrWhiteSpace(password))
                {
                    Console.WriteLine("Please enter the Postgres password:");
                    password = RedflyConsole.GetPasswordFromUser().ToString() ?? string.Empty;
                }

                while (string.IsNullOrWhiteSpace(pgOutputSlotName))
                {
                    Console.WriteLine("Please enter a name for the Postgres Output Slot for Logical Replication:");
                    Console.WriteLine("Ex: redfly_pgout_slot");
                    pgOutputSlotName = Console.ReadLine() ?? string.Empty;
                }

                while (string.IsNullOrWhiteSpace(publicationName))
                {
                    Console.WriteLine("Please enter a name for the  Postgres Publication Name for Logical Replication:");
                    Console.WriteLine("Ex: redfly_publication");
                    publicationName = Console.ReadLine() ?? string.Empty;
                }
            }
            // Verify that we can connect to the database
            while (!RedflyPostgres.VerifyConnectivity(serverName, databaseName, userName, password));

            AppDbSession.PostgresDatabase = SaveDatabaseDetailsToLocalStorage(serverName, databaseName, userName, password, pgOutputSlotName, publicationName);

            return (AppDbSession.PostgresDatabase != null);
        }

        public static bool SelectFromLocalStorage()
        {
            var collection = new LitePostgresDatabaseCollection();
            var all = collection.All();

            if (all.Count() == 0)
            {
                Console.WriteLine("No database details found in local storage.");
                return false;
            }
            else
            {
                Console.WriteLine("\r\nSome databases are available in Local Storage.");
                Console.WriteLine("Do you want to select one of these? (y/n)");
                var response = Console.ReadLine();

                if (response == null ||
                    !response.Equals("y", StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }

                Console.WriteLine("Please select the database to sync from:");

                string? selected = null;
                int selectedIndex = 0;

                do
                {
                    var index = 1;
                    foreach (var item in all)
                    {
                        Console.WriteLine($"({index}) Host: {RedflyEncryption.Decrypt(item.EncryptedServerName)}, Database: {RedflyEncryption.Decrypt(item.EncryptedDatabaseName)}, User Name: @{RedflyEncryption.Decrypt(item.EncryptedUserName)}");
                        index++;
                    }

                    selected = Console.ReadLine();
                }
                while (!int.TryParse(selected, out selectedIndex) ||
                       selectedIndex <= 0 ||
                       selectedIndex > all.Count());

                AppDbSession.PostgresDatabase = all.ElementAt(selectedIndex - 1);
                return true;
            }
        }

        private static LitePostgresDatabaseDocument? SaveDatabaseDetailsToLocalStorage(
            string serverName, 
            string databaseName, 
            string userName, 
            string password,
            string pgOutputSlotName,
            string publicationName)
        {
            var collection = new LitePostgresDatabaseCollection();

            var found = collection.Find(serverName, databaseName, userName);

            if (found == null)
            {
                var document = new LitePostgresDatabaseDocument
                {
                    EncryptedServerName = RedflyEncryption.EncryptToString(serverName),
                    EncryptedDatabaseName = RedflyEncryption.EncryptToString(databaseName),
                    EncryptedUserName = RedflyEncryption.EncryptToString(userName),
                    EncryptedPassword = RedflyEncryption.EncryptToString(password),
                    EncryptedPgOutputSlotName = RedflyEncryption.EncryptToString(pgOutputSlotName),
                    EncryptedPublicationName = RedflyEncryption.EncryptToString(publicationName),
                };

                collection.Add(document);

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Added Postgres Database details to encrypted local storage.");
                Console.ResetColor();

                return document;
            }
            else
            {
                var changed = false;

                if (found.EncryptedServerName != RedflyEncryption.EncryptToString(serverName))
                {
                    found.EncryptedServerName = RedflyEncryption.EncryptToString(serverName);
                    changed = true;
                }

                if (found.EncryptedDatabaseName != RedflyEncryption.EncryptToString(databaseName))
                {
                    found.EncryptedDatabaseName = RedflyEncryption.EncryptToString(databaseName);
                    changed = true;
                }

                if (found.EncryptedUserName != RedflyEncryption.EncryptToString(userName))
                {
                    found.EncryptedUserName = RedflyEncryption.EncryptToString(userName);
                    changed = true;
                }

                if (found.EncryptedPassword != RedflyEncryption.EncryptToString(password))
                {
                    found.EncryptedPassword = RedflyEncryption.EncryptToString(password);
                    changed = true;
                }

                if (found.EncryptedPgOutputSlotName != RedflyEncryption.EncryptToString(pgOutputSlotName))
                {
                    found.EncryptedPgOutputSlotName = RedflyEncryption.EncryptToString(pgOutputSlotName);
                    changed = true;
                }

                if (found.EncryptedPublicationName != RedflyEncryption.EncryptToString(publicationName))
                {
                    found.EncryptedPublicationName = RedflyEncryption.EncryptToString(publicationName);
                    changed = true;
                }

                if (changed)
                {
                    collection.Update(found);

                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("Updated Postgres Database details in encrypted local storage.");
                    Console.ResetColor();
                }

                return found;
            }
        }

    }
}
