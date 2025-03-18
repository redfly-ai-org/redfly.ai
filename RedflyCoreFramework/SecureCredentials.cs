namespace RedflyCoreFramework;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

public class SecureCredentials
{

    private static readonly string CredentialsFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "redfly-credentials.encrypt");
    
    public static bool Exist()
    {
        return File.Exists(CredentialsFilePath);
    }

    public static void Save(string userName, StringBuilder password)
    {
        var credentials = $"{userName}:{password}";
        var encryptedCredentials = RedflyEncryption.EncryptToBytes(credentials, RedflyEncryptionKeys.NativeAesKey);
        File.WriteAllBytes(CredentialsFilePath, encryptedCredentials);
    }

    public static (string userName, StringBuilder password) Get()
    {
        var encryptedCredentials = File.ReadAllBytes(CredentialsFilePath);
        var decryptedCredentials = RedflyEncryption.Decrypt(encryptedCredentials, RedflyEncryptionKeys.NativeAesKey);
        var parts = decryptedCredentials.Split(':');
        return (parts[0], new StringBuilder(parts[1]));
    }

}
