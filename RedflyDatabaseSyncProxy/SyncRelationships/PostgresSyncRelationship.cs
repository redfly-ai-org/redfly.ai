using LiteDB;
using RedflyLocalStorage.Collections;
using RedflyLocalStorage.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedflyDatabaseSyncProxy.SyncRelationships;
internal class PostgresSyncRelationship
{

    internal static void FindExistingRelationshipWithRedis(LiteRedisServerCollection redisServerCollection)
    {
        var postgresSyncRelationshipCollection = new LitePostgresSyncRelationshipCollection();

        var postgresSyncRelationship = postgresSyncRelationshipCollection
                                    .FindByDatabase(AppSession.PostgresDatabase!.Id.ToString()).FirstOrDefault();

        if (postgresSyncRelationship == null)
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

            postgresSyncRelationship = CreateSyncRelationship(postgresSyncRelationshipCollection);
        }

        AppSession.RedisServer = redisServerCollection
                                      .FindById(new BsonValue(new ObjectId(postgresSyncRelationship.RedisServerId)));

        Console.WriteLine($"This Postgres database has a sync relationship with {AppSession.RedisServer.DecryptedServerName}:{AppSession.RedisServer.Port}");
    }

    private static LitePostgresSyncRelationshipDocument CreateSyncRelationship(LitePostgresSyncRelationshipCollection postgresSyncRelationshipCollection)
    {
        LitePostgresSyncRelationshipDocument syncRelationship = new()
        {
            PostgresDatabaseId = AppSession.PostgresDatabase!.Id.ToString(),
            RedisServerId = AppSession.RedisServer!.Id.ToString()
        };
        postgresSyncRelationshipCollection.Add(syncRelationship);

        Console.WriteLine($"The local sync relationship with this Redis Server has been saved successfully");
        return syncRelationship;
    }

}
