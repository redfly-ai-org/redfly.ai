using RedflyLocalStorage.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace redflyDatabaseAdapters
{
    public class AppDbSession
    {

        public static LiteSqlServerDatabaseDocument? SqlServerDatabase { get; set; }

        public static LitePostgresDatabaseDocument? PostgresDatabase { get; set; }

        public static LiteMongoDatabaseDocument? MongoDatabase { get; set; }

        public static LiteRedisServerDocument? RedisServer { get; set; }

    }
}
