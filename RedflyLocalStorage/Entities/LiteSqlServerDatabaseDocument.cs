using LiteDB;
using RedflyCoreFramework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedflyLocalStorage.Entities
{
    public class LiteSqlServerDatabaseDocument
    {

        [BsonId]
        public ObjectId Id { get; set; }

        public required string EncryptedServerName { get; set; }

        public required string EncryptedDatabaseName { get; set; }

        public required string EncryptedUserName { get; set; }

        public required string EncryptedPassword { get; set; }

        public bool DatabasePrepped { get; set; } = false;

        public string ConnectionString
        {
            get
            {
                return $"Server={RedflyEncryption.Decrypt(EncryptedServerName)};Database={RedflyEncryption.Decrypt(EncryptedDatabaseName)};User Id={RedflyEncryption.Decrypt(EncryptedUserName)};Password={RedflyEncryption.Decrypt(EncryptedPassword)};";
            }
        }

        public string DecryptedServerName
        {
            get
            {
                return RedflyEncryption.Decrypt(EncryptedServerName);
            }
        }

        public string DecryptedDatabaseName
        {
            get
            {
                return RedflyEncryption.Decrypt(EncryptedDatabaseName);
            }
        }

        public string DecryptedUserName
        {
            get
            {
                return RedflyEncryption.Decrypt(EncryptedUserName);
            }
        }

    }
}
