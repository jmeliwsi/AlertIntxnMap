using System;
using System.Collections.Generic;
using System.Text;
using System.Security.Cryptography;

namespace FUL
{
    public class Encryption
    {
        private static string key = "4D93EA9E-2872-11DB-8AF6-B622A1EF5492";
        private static string adminKey = "19780117";

        public static string Encrypt(string data)
        {
            byte[] keyArray;
            byte[] toEncryptArray = UTF8Encoding.UTF8.GetBytes(data);
            MD5CryptoServiceProvider hashmd5 = new MD5CryptoServiceProvider();
            keyArray = hashmd5.ComputeHash(UTF8Encoding.UTF8.GetBytes(key));
            hashmd5.Clear();
            TripleDESCryptoServiceProvider tdes = new TripleDESCryptoServiceProvider();
            tdes.Key = keyArray;
            tdes.Mode = CipherMode.ECB;
            tdes.Padding = PaddingMode.PKCS7;
            ICryptoTransform cTransform = tdes.CreateEncryptor();
            byte[] resultArray = cTransform.TransformFinalBlock(toEncryptArray, 0, toEncryptArray.Length);
            tdes.Clear();
            return Convert.ToBase64String(resultArray, 0, resultArray.Length);
        }

        public static string Decrypt(string data)
        {
            byte[] keyArray;
            byte[] toEncryptArray = Convert.FromBase64String(data);
            MD5CryptoServiceProvider hashmd5 = new MD5CryptoServiceProvider();
            keyArray = hashmd5.ComputeHash(UTF8Encoding.UTF8.GetBytes(key));
            hashmd5.Clear();
            TripleDESCryptoServiceProvider tdes = new TripleDESCryptoServiceProvider();
            tdes.Key = keyArray;
            tdes.Mode = CipherMode.ECB;
            tdes.Padding = PaddingMode.PKCS7;
            ICryptoTransform cTransform = tdes.CreateDecryptor();
            byte[] resultArray = cTransform.TransformFinalBlock(toEncryptArray, 0, toEncryptArray.Length);
            tdes.Clear();
            return UTF8Encoding.UTF8.GetString(resultArray);
        }

        /// <summary>
        /// Convert a string input to a MD5 hash, which is a 32-character string of hexadecimal numbers.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static string CalculateMD5Hash(string input)
        {
            // calculate MD5 hash from input
            MD5 md5 = System.Security.Cryptography.MD5.Create();
            byte[] inputBytes = System.Text.Encoding.ASCII.GetBytes(input);
            byte[] hash = md5.ComputeHash(inputBytes);

            // convert byte array to hex string
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < hash.Length; i++)
            {
                sb.Append(hash[i].ToString("X2"));  // X2 indicates upper class characters.
            }
            return sb.ToString();
        } // End CalculateMD5Hash

        public static string EncryptFusion_AdminPassword(string data)
        {
            System.Text.UTF8Encoding encoder = new System.Text.UTF8Encoding();
            HashAlgorithm md5Hasher = new MD5CryptoServiceProvider();
            byte[] byteHashedPassword = new Byte[256];
            byteHashedPassword = md5Hasher.ComputeHash(encoder.GetBytes(data + adminKey));
            return Convert.ToBase64String(byteHashedPassword);
        }
    }
}
