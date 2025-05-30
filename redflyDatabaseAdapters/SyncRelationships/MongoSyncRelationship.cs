﻿using LiteDB;
using redflyDatabaseAdapters;
using RedflyLocalStorage.Collections;
using RedflyLocalStorage.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace redflyDatabaseAdapters;
public class MongoSyncRelationship
{

    public static void FindExistingRelationshipWithRedis(LiteRedisServerCollection redisServerCollection)
    {
        var mongoSyncRelationshipCollection = new LiteMongoSyncRelationshipCollection();

        var mongoSyncRelationship = mongoSyncRelationshipCollection
                                    .FindByDatabase(AppDbSession.MongoDatabase!.Id.ToString()).FirstOrDefault();

        if (mongoSyncRelationship == null)
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

            mongoSyncRelationship = CreateSyncRelationship(mongoSyncRelationshipCollection);
        }

        AppDbSession.RedisServer = redisServerCollection
                                      .FindById(new BsonValue(new ObjectId(mongoSyncRelationship.RedisServerId)));

        Console.WriteLine($"This Mongo database has a sync relationship with {AppDbSession.RedisServer.DecryptedServerName}:{AppDbSession.RedisServer.Port}");
    }

    private static LiteMongoSyncRelationshipDocument CreateSyncRelationship(LiteMongoSyncRelationshipCollection mongoSyncRelationshipCollection)
    {
        LiteMongoSyncRelationshipDocument syncRelationship = new()
        {
            MongoDatabaseId = AppDbSession.MongoDatabase!.Id.ToString(),
            RedisServerId = AppDbSession.RedisServer!.Id.ToString()
        };
        mongoSyncRelationshipCollection.Add(syncRelationship);

        Console.WriteLine($"The local sync relationship with this Redis Server has been saved successfully");
        return syncRelationship;
    }

}
