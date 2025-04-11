using RedflyLocalStorage.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedflyDatabaseSyncProxy
{
    internal class AppSession
    {

        internal static LiteSqlServerDatabaseDocument? SqlServerDatabase { get; set; }

        internal static LitePostgresDatabaseDocument? PostgresDatabase { get; set; }

        internal static LiteRedisServerDocument? RedisServer { get; set; }

        internal static SyncProfileViewModel? SyncProfile { get; set; }

        internal static AddClientAndUserProfileViewModel? ClientAndUserProfileViewModel { get; set; }

    }
}
