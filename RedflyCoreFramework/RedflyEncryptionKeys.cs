namespace RedflyCoreFramework;

using System.Security.Cryptography;

public static class RedflyEncryptionKeys
{

    /// <summary>
    /// The customer's encryption key. Change it to something else before running the apps.
    /// </summary>
    public const string AesKey = "/cjaaIYcB2E4GsPd+wXSWB6HBorzZY86eVuLBrYw0SY=";

    /// <summary>
    /// The customer's encryption key. Change it to something else before running the apps.
    /// </summary>
    public static readonly byte[] NativeAesKey = Convert.FromBase64String(AesKey); // Use a secure key

    public static byte[] GenerateForAes(int keySize = 256)
    {
        using var aes = Aes.Create();
        aes.KeySize = keySize;
        aes.GenerateKey();
        return aes.Key;
    }
}
