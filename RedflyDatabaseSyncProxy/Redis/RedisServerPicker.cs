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
using redflyDatabaseAdapters;

namespace RedflyDatabaseSyncProxy
{
    internal class RedisServerPicker
    {

        internal static bool GetFromUser()
        {
            string serverName = "";
            string password = "";
            string portText = "";
            int port = 6380;

            do
            {
                while (string.IsNullOrWhiteSpace(serverName))
                {
                    Console.WriteLine("Please enter the server name:");
                    serverName = Console.ReadLine() ?? string.Empty;
                }

                while (string.IsNullOrWhiteSpace(password))
                {
                    Console.WriteLine("Please enter the password:");
                    password = RedflyConsole.GetPasswordFromUser().ToString() ?? string.Empty;
                }

                while (string.IsNullOrWhiteSpace(portText) ||
                       !int.TryParse(portText, out port))
                {
                    Console.WriteLine("Please enter the port:");
                    portText = Console.ReadLine() ?? string.Empty;
                }
            }
            // Verify that we can connect to the database
            while (!RedflyRedisServer.VerifyConnectivity(serverName, password, port));

            AppDbSession.RedisServer = SaveDatabaseDetailsToLocalStorage(serverName, password, port);

            return (AppDbSession.RedisServer != null);
        }

        internal static bool SelectFromLocalStorage()
        {
            var collection = new LiteRedisServerCollection();
            var all = collection.All();

            if (all.Count() == 0)
            {
                Console.WriteLine("No Redis Server details found in local storage.");
                return false;
            }
            else
            {
                Console.WriteLine("\r\nSome Redis Servers are available in Local Storage.");
                Console.WriteLine("Do you want to select one of these? (y/n)");
                var response = Console.ReadLine();

                if (response == null ||
                    !response.Equals("y", StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }

                Console.WriteLine("Please select the Redis Server to sync to:");

                string? selected = null;
                int selectedIndex = 0;

                do
                {
                    var index = 1;
                    foreach (var item in all)
                    {
                        Console.WriteLine($"({index}) {RedflyEncryption.Decrypt(item.EncryptedServerName)}:{item.Port}");
                        index++;
                    }

                    selected = Console.ReadLine();
                }
                while (!int.TryParse(selected, out selectedIndex) ||
                       selectedIndex <= 0 ||
                       selectedIndex > all.Count());

                AppDbSession.RedisServer = all.ElementAt(selectedIndex - 1);
                return true;
            }
        }

        private static LiteRedisServerDocument? SaveDatabaseDetailsToLocalStorage(string serverName, string password, int port)
        {
            var collection = new LiteRedisServerCollection();

            var found = collection.Find(serverName);

            if (found == null)
            {
                var document = new LiteRedisServerDocument
                {
                    EncryptedServerName = RedflyEncryption.EncryptToString(serverName),
                    EncryptedPassword = RedflyEncryption.EncryptToString(password),
                    Port = port
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

                if (found.EncryptedPassword != RedflyEncryption.EncryptToString(password))
                {
                    found.EncryptedPassword = RedflyEncryption.EncryptToString(password);
                    changed = true;
                }

                if (found.Port != port)
                {
                    found.Port = port;
                    changed = true;
                }

                if (changed)
                {
                    collection.Update(found);

                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("Updated Redis Server details in encrypted local storage.");
                    Console.ResetColor();
                }

                return found;
            }
        }

    }
}
