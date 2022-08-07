using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace EtsGitTools.Helpers
{
    public static class StringEncryption
    {
        private const string encryptionKey = "167161b4-7236-4e80-8430-59086a00bede";
        private static readonly byte[] salt = Encoding.ASCII.GetBytes(encryptionKey.Length.ToString());

        public static string Encrypt(string str)
        {
            if (!string.IsNullOrWhiteSpace(str))
            {
                RijndaelManaged rijndaelCipher = new RijndaelManaged();
                byte[] plainText = Encoding.Unicode.GetBytes(str);
                PasswordDeriveBytes SecretKey = new PasswordDeriveBytes(encryptionKey, salt);

                using (ICryptoTransform encryptor = rijndaelCipher.CreateEncryptor(SecretKey.GetBytes(32), SecretKey.GetBytes(16)))
                {
                    using (MemoryStream memoryStream = new MemoryStream())
                    {
                        using (CryptoStream cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write))
                        {
                            cryptoStream.Write(plainText, 0, plainText.Length);
                            cryptoStream.FlushFinalBlock();

                            return Convert.ToBase64String(memoryStream.ToArray());
                        }
                    }
                }
            }
            return "";
        }

        public static string Decrypt(string encryptedStr)
        {
            if (!string.IsNullOrWhiteSpace(encryptedStr))
            {
                RijndaelManaged rijndaelCipher = new RijndaelManaged();
                byte[] encryptedData = Convert.FromBase64String(encryptedStr);
                PasswordDeriveBytes secretKey = new PasswordDeriveBytes(encryptionKey, salt);

                using (ICryptoTransform decryptor = rijndaelCipher.CreateDecryptor(secretKey.GetBytes(32), secretKey.GetBytes(16)))
                {
                    using (MemoryStream memoryStream = new MemoryStream(encryptedData))
                    {
                        using (CryptoStream cryptoStream = new CryptoStream(memoryStream, decryptor, CryptoStreamMode.Read))
                        {
                            byte[] plainText = new byte[encryptedData.Length];
                            int decryptedCount = cryptoStream.Read(plainText, 0, plainText.Length);

                            return Encoding.Unicode.GetString(plainText, 0, decryptedCount);
                        }
                    }
                }
            }

            return "";
        }
    }
}
