﻿using RedflyCoreFramework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedflyLocalStorage.Entities;

public class LitePostgresDatabaseDocument : BaseLiteDocument
{

    public required string EncryptedServerName { get; set; }

    public required string EncryptedDatabaseName { get; set; }

    public required string EncryptedUserName { get; set; }

    public required string EncryptedPassword { get; set; }

    public string EncryptedTestDecodingSlotName { get; set; } = RedflyEncryption.EncryptToString("test_decoding_slot");

    public required string EncryptedPgOutputSlotName { get; set; }

    public required string EncryptedPublicationName { get; set; }

    public bool DatabasePrepped { get; set; } = false;

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

    public string GetPassword()
    {
        return RedflyEncryption.Decrypt(EncryptedPassword);
    }

}
