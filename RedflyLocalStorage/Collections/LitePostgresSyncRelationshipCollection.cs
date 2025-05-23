﻿using LiteDB;
using RedflyCoreFramework;
using RedflyLocalStorage.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedflyLocalStorage.Collections;

public class LitePostgresSyncRelationshipCollection : RedflyLocalCollection<LitePostgresSyncRelationshipDocument>
{

    public LitePostgresSyncRelationshipCollection() : base("postgressyncrelationships")
    {
        _lazyCollection.Value.EnsureIndex(
            name: "sqlsrvridrdsid",
            x => new
            {
                x.PostgresDatabaseId,
                x.RedisServerId
            },
            unique: true);
    }

    public IEnumerable<LitePostgresSyncRelationshipDocument> FindByDatabase(string postgresDatabaseId)
    {
        return _lazyCollection.Value
                    .Find(x => x.PostgresDatabaseId == postgresDatabaseId);
    }

    public IEnumerable<LitePostgresSyncRelationshipDocument> FindByRedisServer(string redisServerId)
    {
        return _lazyCollection.Value
                    .Find(x =>
                            x.RedisServerId == redisServerId);
    }

    public LitePostgresSyncRelationshipDocument Find(string postgresDatabaseId, string redisServerId)
    {
        return _lazyCollection.Value
                    .FindOne(x =>
                                x.PostgresDatabaseId == postgresDatabaseId &&
                                x.RedisServerId == redisServerId);
    }

}
