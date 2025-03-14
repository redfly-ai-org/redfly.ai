using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace RedflyCoreFramework
{
    public class RedflyEncryption
    {

        public static string EncryptToString(string plainText)
        {
            return EncryptToString(plainText, RedflyEncryptionKeys.AesKey);
        }

        public static string EncryptToString(string plainText, byte[] key)
        {
            var encryptedBytes = EncryptToBytes(plainText, key);
            return Convert.ToBase64String(encryptedBytes);
        }

        public static byte[] EncryptToBytes(string plainText)
        {
            return EncryptToBytes(plainText, RedflyEncryptionKeys.AesKey);
        }

        public static byte[] EncryptToBytes(string plainText, byte[] key)
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

        public static string Decrypt(string encryptedText)
        {
            var cipherBytes = Convert.FromBase64String(encryptedText);
            return Decrypt(cipherBytes);
        }

        public static string Decrypt(byte[] cipherText)
        {
            return Decrypt(cipherText, RedflyEncryptionKeys.AesKey);
        }

        public static string Decrypt(byte[] cipherText, byte[] key)
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
}
