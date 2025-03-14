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
    private static readonly byte[] EncryptionKey = Convert.FromBase64String("/cjaaIYcB2E4GsPd+wXSWB6HBorzZY86eVuLBrYw0SY="); // Use a secure key

    public static bool Exist()
    {
        return File.Exists(CredentialsFilePath);
    }

    public static void Save(string userName, StringBuilder password)
    {
        var credentials = $"{userName}:{password}";
        var encryptedCredentials = Encrypt(credentials, EncryptionKey);
        File.WriteAllBytes(CredentialsFilePath, encryptedCredentials);
    }

    public static (string userName, StringBuilder password) Get()
    {
        var encryptedCredentials = File.ReadAllBytes(CredentialsFilePath);
        var decryptedCredentials = Decrypt(encryptedCredentials, EncryptionKey);
        var parts = decryptedCredentials.Split(':');
        return (parts[0], new StringBuilder(parts[1]));
    }

    private static byte[] Encrypt(string plainText, byte[] key)
    {
        using var aes = Aes.Create();
        aes.Key = key;
        aes.GenerateIV();
        using var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
        using var ms = new MemoryStream();
        ms.Write(aes.IV, 0, aes.IV.Length);
        using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
        using (var sw = new StreamWriter(cs))
        {
            sw.Write(plainText);
        }
        return ms.ToArray();
    }

    private static string Decrypt(byte[] cipherText, byte[] key)
    {
        using var aes = Aes.Create();
        aes.Key = key;
        using var ms = new MemoryStream(cipherText);
        var iv = new byte[aes.IV.Length];
        ms.Read(iv, 0, iv.Length);
        using var decryptor = aes.CreateDecryptor(aes.Key, iv);
        using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
        using var sr = new StreamReader(cs);
        return sr.ReadToEnd();
    }

}
