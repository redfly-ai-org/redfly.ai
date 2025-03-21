﻿using LiteDB;
using RedflyCoreFramework;
using RedflyLocalStorage.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedflyLocalStorage.Collections
{
    public class LiteSyncRelationshipCollection : RedflyLocalCollection<LiteSyncRelationshipDocument>
    {

        public LiteSyncRelationshipCollection() : base("syncrelationships")
        {
            _lazyCollection.Value.EnsureIndex(
                name: "sqlsrvridrdsid",
                x => new
                {
                    x.SqlServerDatabaseId,
                    x.RedisServerId
                },
                unique: true);
        }

        public IEnumerable<LiteSyncRelationshipDocument> FindByDatabase(string sqlServerDatabaseId)
        {
            return _lazyCollection.Value
                        .Find(x => x.SqlServerDatabaseId == sqlServerDatabaseId);
        }

        public IEnumerable<LiteSyncRelationshipDocument> FindByRedisServer(string redisServerId)
        {
            return _lazyCollection.Value
                        .Find(x =>
                                x.RedisServerId == redisServerId);
        }

        public LiteSyncRelationshipDocument Find(string sqlServerDatabaseId, string redisServerId)
        {
            return _lazyCollection.Value
                        .FindOne(x =>
                                    x.SqlServerDatabaseId == sqlServerDatabaseId &&
                                    x.RedisServerId == redisServerId);
        }

    }
}
