using LiteDB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedflyLocalStorage.Entities;

public class LitePostgresSyncRelationshipDocument : BaseLiteDocument
{

    public required string PostgresDatabaseId { get; set; }

    public required string RedisServerId { get; set; }

}
