using LiteDB;
using RedflyLocalStorage.Collections;
using RedflyLocalStorage.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedflyDatabaseSyncProxy.SyncRelationships;
internal class SqlServerSyncRelationship
{

    internal static void FindExistingRelationshipWithRedis(LiteRedisServerCollection redisServerCollection)
    {
        var sqlServerSyncRelationshipCollection = new LiteSqlServerSyncRelationshipCollection();

        var sqlServerSyncRelationship = sqlServerSyncRelationshipCollection
                                    .FindByDatabase(AppSession.SqlServerDatabase!.Id.ToString()).FirstOrDefault();

        if (sqlServerSyncRelationship == null)
        {
            if (!RedisServerPicker.SelectFromLocalStorage())
            {
                if (!RedisServerPicker.GetFromUser())
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Chakra Sync cannot be started without selecting a target Redis Server.");
                    Console.WriteLine("Please select a Redis Server and try again.");
                    Console.ResetColor();
                    return;
                }
            }

            sqlServerSyncRelationship = CreateSyncRelationship(sqlServerSyncRelationshipCollection);
        }

        AppSession.RedisServer = redisServerCollection
                                      .FindById(new BsonValue(new ObjectId(sqlServerSyncRelationship.RedisServerId)));

        Console.WriteLine($"This Sql Server database has a sync relationship with {AppSession.RedisServer.DecryptedServerName}:{AppSession.RedisServer.Port}");
    }

    private static LiteSqlServerSyncRelationshipDocument CreateSyncRelationship(LiteSqlServerSyncRelationshipCollection sqlServerSyncRelationshipCollection)
    {
        LiteSqlServerSyncRelationshipDocument syncRelationship = new()
        {
            SqlServerDatabaseId = AppSession.SqlServerDatabase!.Id.ToString(),
            RedisServerId = AppSession.RedisServer!.Id.ToString()
        };
        sqlServerSyncRelationshipCollection.Add(syncRelationship);

        Console.WriteLine($"The local sync relationship with this Redis Server has been saved successfully");
        return syncRelationship;
    }


}
