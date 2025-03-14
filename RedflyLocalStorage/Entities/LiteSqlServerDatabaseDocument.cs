using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedflyLocalStorage.Entities
{
    public class LiteSqlServerDatabaseDocument
    {

        public required string EncryptedServerName { get; set; }

        public required string EncryptedDatabaseName { get; set; }

        public required string EncryptedUserName { get; set; }

        public required string EncryptedPassword { get; set; }

        public bool DatabasePrepped { get; set; } = false;

    }
}
