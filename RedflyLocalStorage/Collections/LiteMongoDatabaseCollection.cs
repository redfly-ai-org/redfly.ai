using LiteDB;
using RedflyCoreFramework;
using RedflyLocalStorage.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedflyLocalStorage.Collections;

public class LiteMongoDatabaseCollection : RedflyLocalCollection<LiteMongoDatabaseDocument>
{

    public LiteMongoDatabaseCollection() : base("mongodatabases")
    {
        //Sometimes users see differently based on access (future TODO).
        _lazyCollection.Value.EnsureIndex(
            name: "srvrnmdbnmusnm",
            x => new
            {
                x.EncryptedServerName,
                x.EncryptedDatabaseName,
                x.EncryptedUserName
            },
            unique: true);
    }

    public IEnumerable<LiteMongoDatabaseDocument> Find(string encryptedServerName)
    {
        return _lazyCollection.Value
                    .Find(x => x.EncryptedServerName == encryptedServerName);
    }

    public IEnumerable<LiteMongoDatabaseDocument> Find(string encryptedServerName, string encryptedDatabaseName)
    {
        return _lazyCollection.Value
                    .Find(x =>
                            x.EncryptedServerName == encryptedServerName &&
                            x.EncryptedDatabaseName == encryptedDatabaseName);
    }

    public LiteMongoDatabaseDocument Find(string encryptedServerName, string encryptedDatabaseName, string encryptedUserName)
    {
        return _lazyCollection.Value
                    .FindOne(x =>
                                x.EncryptedServerName == encryptedServerName &&
                                x.EncryptedDatabaseName == encryptedDatabaseName &&
                                x.EncryptedUserName == encryptedUserName);
    }

}
