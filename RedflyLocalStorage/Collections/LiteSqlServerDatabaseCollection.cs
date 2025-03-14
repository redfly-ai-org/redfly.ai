using LiteDB;
using RedflyCoreFramework;
using RedflyLocalStorage.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedflyLocalStorage.Collections
{
    public class LiteSqlServerDatabaseCollection : RedflyLocalCollection<LiteSqlServerDatabaseDocument>
    {

        public LiteSqlServerDatabaseCollection(LiteDatabase db) : base(db, "sqlserverdatabases")
        {
            //Sometimes users see differently based on access (future TODO).
            _collection.EnsureIndex(
                name: "srvrnmdbnmusnm",
                x => new 
                { 
                    x.EncryptedServerName, 
                    x.EncryptedDatabaseName, 
                    x.EncryptedUserName 
                }, 
                unique: true);
        }

        public IEnumerable<LiteSqlServerDatabaseDocument> Find(string serverName)
        {
            return _collection
                        .Find(x => x.EncryptedServerName == RedflyEncryption
                                                            .EncryptToString(serverName));
        }

        public IEnumerable<LiteSqlServerDatabaseDocument> Find(string serverName, string databaseName)
        {
            return _collection
                        .Find(x =>    
                                x.EncryptedServerName == RedflyEncryption
                                                            .EncryptToString(serverName) &&
                                x.EncryptedDatabaseName == RedflyEncryption
                                                            .EncryptToString(databaseName));
        }

        public LiteSqlServerDatabaseDocument Find(string serverName, string databaseName, string userName)
        {
            return _collection
                        .FindOne(x =>
                                    x.EncryptedServerName == RedflyEncryption
                                                                .EncryptToString(serverName) &&
                                    x.EncryptedDatabaseName == RedflyEncryption
                                                                .EncryptToString(databaseName) &&
                                    x.EncryptedUserName == RedflyEncryption
                                                                .EncryptToString(userName));
        }

    }
}
