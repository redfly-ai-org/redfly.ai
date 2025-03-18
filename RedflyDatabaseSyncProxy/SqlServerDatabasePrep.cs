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
    internal class SqlServerDatabasePrep
    {

        internal static bool ForChangeManagement()
        {
            if (!SqlServerDatabasePicker.SelectFromLocalStorage())
            {
                if (!SqlServerDatabasePicker.GetFromUser())
                {
                    return false;
                }
            }

            if (AppSession.Database != null &&
                AppSession.Database.DatabasePrepped)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("This database has already been prepped for redfly.");
                Console.ResetColor();
                return true;
            }

            if (!EnableDatabaseChangeTracking())
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Failed to enable change tracking for this database.");
                Console.ResetColor();

                return false;
            }

            if (!AllowDatabaseSnapshotIsolation())
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Failed to enable snapshot isolation for this database.");
                Console.ResetColor();

                return false;
            }

            if (!AddVersionColumnAndEnableChangeTracking_ForAllSupportedTables())
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Failed to add Version column to all supported tables.");
                Console.ResetColor();

                return false;
            }

           if (AppSession.Database != null)
           {
                var collection = new LiteSqlServerDatabaseCollection();

                var row = collection.Find(AppSession.Database.EncryptedServerName, 
                                          AppSession.Database.EncryptedDatabaseName, 
                                          AppSession.Database.EncryptedUserName);

                row.DatabasePrepped = true;
                collection.Update(row);

                AppSession.Database = row;
            }
            
            return true;
        }

        private static bool EnableDatabaseChangeTracking()
        {
            var selectedDatabase = AppSession.Database;

            if (selectedDatabase == null)
            {
                return false;
            }

            using var connection = new SqlConnection(selectedDatabase.ConnectionString);
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = "SELECT database_id FROM sys.change_tracking_databases WHERE database_id = DB_ID()";
            var result = command.ExecuteScalar();

            if (result != null)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Change tracking is already enabled for this database.");
                Console.ResetColor();

                return true;
            }

            command.CommandText = "ALTER DATABASE " + selectedDatabase.DecryptedDatabaseName + " SET CHANGE_TRACKING = ON (CHANGE_RETENTION = 2 DAYS, AUTO_CLEANUP = ON)";
            command.ExecuteNonQuery();

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Change tracking has been enabled for this database.");
            Console.ResetColor();

            return true;
        }

        private static bool AllowDatabaseSnapshotIsolation()
        {
            var selectedDatabase = AppSession.Database;

            if (selectedDatabase == null)
            {
                return false;
            }

            using var connection = new SqlConnection(selectedDatabase.ConnectionString);
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT snapshot_isolation_state_desc FROM sys.databases WHERE name = DB_NAME()";

            var result = command.ExecuteScalar();
            if (result != null && result.ToString() == "ON")
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Snapshot isolation is already enabled for this database.");
                Console.ResetColor();

                return true;
            }

            command.CommandText = "ALTER DATABASE " + selectedDatabase.DecryptedDatabaseName + " SET ALLOW_SNAPSHOT_ISOLATION ON";
            command.ExecuteNonQuery();

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Snapshot isolation has been enabled for this database.");
            Console.ResetColor();

            return true;
        }

        /// <summary>
        /// A method which adds a timestamp column named "Version" to every table in the 
        /// database which has a primary key and which is NOT a memory optimized table.
        /// </summary>
        private static bool AddVersionColumnAndEnableChangeTracking_ForAllSupportedTables()
        {
            var selectedDatabase = AppSession.Database;

            if (selectedDatabase == null)
            {
                return false;
            }

            using var connection = new SqlConnection(selectedDatabase.ConnectionString);
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT name FROM sys.tables WHERE is_memory_optimized = 0";

            var tables = new List<string>();
            SqlDataReader reader;

            using (reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    tables.Add(reader.GetString(0));
                }
            }

            foreach (var table in tables)
            {
                command.CommandText = @"
                    SELECT i.name 
                    FROM sys.indexes i
                    INNER JOIN sys.index_columns ic ON i.object_id = ic.object_id AND i.index_id = ic.index_id
                    WHERE i.is_primary_key = 1 AND i.object_id = OBJECT_ID('" + table + "')";

                using (reader = command.ExecuteReader())
                {
                    if (!reader.Read())
                    {
                        continue;
                    }
                }

                command.CommandText = "SELECT name FROM sys.columns WHERE object_id = OBJECT_ID('" + table + "') AND name = 'Version'";

                using (reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        continue;
                    }
                }

                command.CommandText = "ALTER TABLE " + table + " ADD Version timestamp";
                command.ExecuteNonQuery();

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Added Version column to table " + table);
                Console.ResetColor();

                // Check if change tracking is already enabled for the table
                command.CommandText = "SELECT is_track_columns_updated_on FROM sys.change_tracking_tables WHERE object_id = OBJECT_ID('" + table + "')";
                var changeTrackingEnabled = command.ExecuteScalar();

                if (changeTrackingEnabled == null)
                {
                    // Enable change tracking for the table
                    command.CommandText = "ALTER TABLE " + table + " ENABLE CHANGE_TRACKING WITH (TRACK_COLUMNS_UPDATED = ON)";
                    command.ExecuteNonQuery();

                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("Enabled change tracking for table " + table);
                    Console.ResetColor();
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("Change tracking is already enabled for table " + table);
                    Console.ResetColor();
                }
            }
            return true;
        }

    }
}
