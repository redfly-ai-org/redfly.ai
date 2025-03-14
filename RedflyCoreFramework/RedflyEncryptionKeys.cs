namespace RedflyCoreFramework;

using System.Security.Cryptography;

public static class RedflyEncryptionKeys
{

    public static readonly byte[] AesKey = Convert.FromBase64String("/cjaaIYcB2E4GsPd+wXSWB6HBorzZY86eVuLBrYw0SY="); // Use a secure key

    public static byte[] GenerateForAes(int keySize = 256)
    {
        using var aes = Aes.Create();
        aes.KeySize = keySize;
        aes.GenerateKey();
        return aes.Key;
    }
}
