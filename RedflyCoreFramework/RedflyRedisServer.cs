using Microsoft.Data.SqlClient;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedflyCoreFramework
{
    public class RedflyRedisServer
    {
        public static bool VerifyConnectivity(string? serverName, string password, int port)
        {
            // Return false if any of the parameters are null
            if (serverName == null || password == null)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("One or more of the required parameters are null.");
                Console.ResetColor();
                return false;
            }

            // Return false if the port is not a valid port number
            if (port < 1 || port > 65535)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Invalid port number.");
                Console.ResetColor();
                return false;
            }

            // Validate Redis Cache connectivity
            try
            {
                var configurationOptions = new ConfigurationOptions
                {
                    EndPoints = { $"{serverName}:{port}" },
                    Password = password,
                    AbortOnConnectFail = false
                };

                Console.WriteLine("Connecting to Redis Host...");

                using (var connection = ConnectionMultiplexer.Connect(configurationOptions))
                {
                    if (connection.IsConnected)
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine("Successfully connected to the Redis server.");
                        Console.ResetColor();
                        return true;
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("Failed to connect to the Redis server.");
                        Console.ResetColor();
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Exception occurred: {ex.Message}");
                Console.ResetColor();
                return false;
            }
        }
    }
}
