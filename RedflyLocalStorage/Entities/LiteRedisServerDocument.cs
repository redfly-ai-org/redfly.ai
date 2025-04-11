using RedflyCoreFramework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedflyLocalStorage.Entities;

public class LiteRedisServerDocument : BaseLiteDocument
{

    public required string EncryptedServerName { get; set; }
    public required string EncryptedPassword { get; set; }        
    public required int Port { get; set; }

    public bool UsesSsl { get; set; } = true;
    public string SslProtocol { get; set; } = "tls12";
    public bool AbortConnect { get; set; } = false;
    public int ConnectTimeout { get; set; } = 8000;
    public int SyncTimeout { get; set; } = 8000;
    public int AsyncTimeout { get; set; } = 8000;

    public string DecryptedServerName
    {
        get
        {
            return RedflyEncryption.Decrypt(EncryptedServerName);
        }
    }

}
