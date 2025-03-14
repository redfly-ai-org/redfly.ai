using LiteDB;
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

            Console.WriteLine("Have you prepped this database for redfly? (y/n)");
            var response = Console.ReadLine();

            if (string.Equals(response, "y", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return true;
        }

    }
}
