using LiteDB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedflyLocalStorage.Entities;

public class LiteMongoSyncRelationshipDocument : BaseLiteDocument
{

    public required string MongoDatabaseId { get; set; }

    public required string RedisServerId { get; set; }

}
