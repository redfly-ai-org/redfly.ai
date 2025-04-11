using LiteDB;
using RedflyCoreFramework;
using RedflyLocalStorage.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedflyLocalStorage.Collections;

public class LitePostgresDatabaseCollection : RedflyLocalCollection<LitePostgresDatabaseDocument>
{

    public LitePostgresDatabaseCollection() : base("postgresdatabases")
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

    public IEnumerable<LitePostgresDatabaseDocument> Find(string encryptedServerName)
    {
        return _lazyCollection.Value
                    .Find(x => x.EncryptedServerName == encryptedServerName);
    }

    public IEnumerable<LitePostgresDatabaseDocument> Find(string encryptedServerName, string encryptedDatabaseName)
    {
        return _lazyCollection.Value
                    .Find(x =>
                            x.EncryptedServerName == encryptedServerName &&
                            x.EncryptedDatabaseName == encryptedDatabaseName);
    }

    public LitePostgresDatabaseDocument Find(string encryptedServerName, string encryptedDatabaseName, string encryptedUserName)
    {
        return _lazyCollection.Value
                    .FindOne(x =>
                                x.EncryptedServerName == encryptedServerName &&
                                x.EncryptedDatabaseName == encryptedDatabaseName &&
                                x.EncryptedUserName == encryptedUserName);
    }

}
