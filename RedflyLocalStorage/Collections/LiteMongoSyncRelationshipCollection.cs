using LiteDB;
using RedflyCoreFramework;
using RedflyLocalStorage.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedflyLocalStorage.Collections;

public class LiteMongoSyncRelationshipCollection : RedflyLocalCollection<LiteMongoSyncRelationshipDocument>
{

    public LiteMongoSyncRelationshipCollection() : base("mongosyncrelationships")
    {
        _lazyCollection.Value.EnsureIndex(
            name: "sqlsrvridrdsid",
            x => new
            {
                x.MongoDatabaseId,
                x.RedisServerId
            },
            unique: true);
    }

    public IEnumerable<LiteMongoSyncRelationshipDocument> FindByDatabase(string mongoDatabaseId)
    {
        return _lazyCollection.Value
                    .Find(x => x.MongoDatabaseId == mongoDatabaseId);
    }

    public IEnumerable<LiteMongoSyncRelationshipDocument> FindByRedisServer(string redisServerId)
    {
        return _lazyCollection.Value
                    .Find(x =>
                            x.RedisServerId == redisServerId);
    }

    public LiteMongoSyncRelationshipDocument Find(string mongoDatabaseId, string redisServerId)
    {
        return _lazyCollection.Value
                    .FindOne(x =>
                                x.MongoDatabaseId == mongoDatabaseId &&
                                x.RedisServerId == redisServerId);
    }

}
