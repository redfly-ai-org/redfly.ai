﻿using LiteDB;
using RedflyCoreFramework;
using RedflyLocalStorage.Collections;
using RedflyLocalStorage;
using RedflyLocalStorage.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedflyDatabaseSyncProxy
{
    internal class SqlServerDatabasePicker
    {

        internal static LiteSqlServerDatabaseDocument? SelectedDatabase { get; set; }

        internal static bool GetFromUser()
        {
            string serverName = "";
            string databaseName = "";
            string userName = "";
            string password = "";

            do
            {
                while (string.IsNullOrWhiteSpace(serverName))
                {
                    Console.WriteLine("Please enter the server name:");
                    serverName = Console.ReadLine() ?? string.Empty;
                }

                while (string.IsNullOrWhiteSpace(databaseName))
                {
                    Console.WriteLine("Please enter the database name:");
                    databaseName = Console.ReadLine() ?? string.Empty;
                }

                while (string.IsNullOrWhiteSpace(userName))
                {
                    Console.WriteLine("Please enter the username:");
                    userName = Console.ReadLine() ?? string.Empty;
                }

                while (string.IsNullOrWhiteSpace(password))
                {
                    Console.WriteLine("Please enter the password:");
                    password = RedflyConsole.GetPasswordFromUser().ToString() ?? string.Empty;
                }
            }
            // Verify that we can connect to the database
            while (!RedflySqlServer.VerifyConnectivity(serverName, databaseName, userName, password));

            SelectedDatabase = SaveDatabaseDetailsToLocalStorage(serverName, databaseName, userName, password);

            return (SelectedDatabase != null);
        }

        internal static bool SelectFromLocalStorage()
        {
            var collection = new LiteSqlServerDatabaseCollection();
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
                        Console.WriteLine($"({index}) {RedflyEncryption.Decrypt(item.EncryptedServerName)} -> {RedflyEncryption.Decrypt(item.EncryptedDatabaseName)}, @{RedflyEncryption.Decrypt(item.EncryptedUserName)}");
                        index++;
                    }

                    selected = Console.ReadLine();
                }
                while (!int.TryParse(selected, out selectedIndex) ||
                       selectedIndex <= 0 ||
                       selectedIndex > all.Count());

                SelectedDatabase = all.ElementAt(selectedIndex - 1);
                return true;
            }
        }

        private static LiteSqlServerDatabaseDocument? SaveDatabaseDetailsToLocalStorage(string serverName, string databaseName, string userName, string password)
        {
            var collection = new LiteSqlServerDatabaseCollection();

            var found = collection.Find(serverName, databaseName, userName);

            if (found == null)
            {
                var document = new LiteSqlServerDatabaseDocument
                {
                    EncryptedServerName = RedflyEncryption.EncryptToString(serverName),
                    EncryptedDatabaseName = RedflyEncryption.EncryptToString(databaseName),
                    EncryptedUserName = RedflyEncryption.EncryptToString(userName),
                    EncryptedPassword = RedflyEncryption.EncryptToString(password)
                };

                collection.Add(document);

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Added Database details to encrypted local storage.");
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

                if (changed)
                {
                    collection.Update(found);

                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("Updated Database details in encrypted local storage.");
                    Console.ResetColor();
                }

                return found;
            }
        }

    }
}
