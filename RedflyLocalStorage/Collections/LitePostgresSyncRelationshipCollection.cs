using LiteDB;
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

    public IEnumerable<LitePostgresSyncRelationshipDocument> FindByDatabase(string sqlServerDatabaseId)
    {
        return _lazyCollection.Value
                    .Find(x => x.PostgresDatabaseId == sqlServerDatabaseId);
    }

    public IEnumerable<LitePostgresSyncRelationshipDocument> FindByRedisServer(string redisServerId)
    {
        return _lazyCollection.Value
                    .Find(x =>
                            x.RedisServerId == redisServerId);
    }

    public LitePostgresSyncRelationshipDocument Find(string sqlServerDatabaseId, string redisServerId)
    {
        return _lazyCollection.Value
                    .FindOne(x =>
                                x.PostgresDatabaseId == sqlServerDatabaseId &&
                                x.RedisServerId == redisServerId);
    }

}
