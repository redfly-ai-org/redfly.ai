namespace RedflyCoreFramework;

using System.Security.Cryptography;

public static class EncryptionKeys
{
    public static byte[] GenerateForAes(int keySize = 256)
    {
        using var aes = Aes.Create();
        aes.KeySize = keySize;
        aes.GenerateKey();
        return aes.Key;
    }
}
