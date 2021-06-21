using UnityEngine;
using System;
using System.Security.Cryptography;
using System.IO;
using System.Text;
using Swift.Core;

namespace Swift.EncryptedPlayerPrefs
{
    [DependsOnModules(typeof(ISoundManager))]
    internal class EncryptedPlayerPrefs : Module, ISecurePrefs
    {
        private const int keyLength = 128;
        private const string SaltKey = "ShMG8hLyZ7k~Ge5@";
        private const string VIKey = "~6YUi0Sv5@|{aOZO";
        private const string password = "h4j2v2-2-2,d-sksd";

        private static string Encrypt(byte[] value, string password)
        {
            var keyBytes = new Rfc2898DeriveBytes(password, Encoding.UTF8.GetBytes(SaltKey)).GetBytes(keyLength / 8);
            var symmetricKey = new RijndaelManaged { Mode = CipherMode.CBC, Padding = PaddingMode.Zeros };
            var encryptor = symmetricKey.CreateEncryptor(keyBytes, Encoding.UTF8.GetBytes(VIKey));

            using (var memoryStream = new MemoryStream())
            {
                using (var cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write))
                {
                    cryptoStream.Write(value, 0, value.Length);
                    cryptoStream.FlushFinalBlock();
                    cryptoStream.Close();
                    memoryStream.Close();

                    return Convert.ToBase64String(memoryStream.ToArray());
                }
            }
        }

        private static string Encrypt(string value, string password)
        {
            return Encrypt(Encoding.UTF8.GetBytes(value), password);
        }

        private static string Decrypt(string value, string password)
        {
            var cipherTextBytes = Convert.FromBase64String(value);
            var keyBytes = new Rfc2898DeriveBytes(password, Encoding.UTF8.GetBytes(SaltKey)).GetBytes(keyLength / 8);
            var symmetricKey = new RijndaelManaged { Mode = CipherMode.CBC, Padding = PaddingMode.None };
            var decryptor = symmetricKey.CreateDecryptor(keyBytes, Encoding.UTF8.GetBytes(VIKey));

            using (var memoryStream = new MemoryStream(cipherTextBytes))
            {
                using (var cryptoStream = new CryptoStream(memoryStream, decryptor, CryptoStreamMode.Read))
                {
                    var plainTextBytes = new byte[cipherTextBytes.Length];
                    var decryptedByteCount = cryptoStream.Read(plainTextBytes, 0, plainTextBytes.Length);

                    memoryStream.Close();
                    cryptoStream.Close();

                    return Encoding.UTF8.GetString(plainTextBytes, 0, decryptedByteCount).TrimEnd("\0".ToCharArray());
                }
            }
        }

        public string GetValue(string key)
        {
            if(Exists(key) == false)
            {
                return default;
            }

            return Decrypt(PlayerPrefs.GetString(key), password);
        }

        public void SetValue(string key, string value)
        {
            PlayerPrefs.SetString(key, Encrypt(value, password));
        }

        public bool Exists(string key)
        {
            return PlayerPrefs.HasKey(key);
        }

        public void Save()
        {
            PlayerPrefs.Save();
        }

        public void Delete(string key)
        {
            PlayerPrefs.DeleteKey(key);
        }
    }
}
