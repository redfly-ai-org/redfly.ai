using RedflyLocalStorage.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedflyLocalStorage.Collections;

public class LiteRedisServerCollection : RedflyLocalCollection<LiteRedisServerDocument>
{

    public LiteRedisServerCollection() : base("redisservers")
    {
        //Sometimes users see differently based on access (future TODO).
        _lazyCollection.Value.EnsureIndex(
            name: "srvrnm",
            x => new
            {
                x.EncryptedServerName
            },
            unique: true);
    }

    public LiteRedisServerDocument Find(string encryptedServerName)
    {
        return _lazyCollection.Value
                    .FindOne(x => x.EncryptedServerName == encryptedServerName);
    }

}
