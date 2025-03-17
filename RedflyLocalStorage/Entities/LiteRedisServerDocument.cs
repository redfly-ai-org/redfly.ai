using RedflyCoreFramework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedflyLocalStorage.Entities
{
    public class LiteRedisServerDocument : BaseLiteDocument
    {

        public required string EncryptedServerName { get; set; }
        public required string EncryptedPassword { get; set; }        
        public required int Port { get; set; }

        public string DecryptedServerName
        {
            get
            {
                return RedflyEncryption.Decrypt(EncryptedServerName);
            }
        }

    }
}
