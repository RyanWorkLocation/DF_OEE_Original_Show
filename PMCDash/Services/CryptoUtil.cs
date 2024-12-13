using Microsoft.AspNetCore.WebUtilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace PMCDash.Services
{
    public class CryptoUtil
    {
        private readonly Aes _aes;
        public CryptoUtil(string key, string iv)
        {
            var aes = Aes.Create();
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;
            aes.Key = SHA256.Create().ComputeHash(Encoding.UTF8.GetBytes(key));
            aes.IV = MD5.Create().ComputeHash(Encoding.UTF8.GetBytes(iv));
            _aes = aes;
        }
        public string Encrypt(string text)
        {
            var sourceBytes = Encoding.UTF8.GetBytes(text);
            var transform = _aes.CreateEncryptor();
            return WebEncoders.Base64UrlEncode(transform.TransformFinalBlock(sourceBytes, 0, sourceBytes.Length));
        }

        public string Decrypt(string text)
        {
            var encryptBytes = WebEncoders.Base64UrlDecode(text);
            var transform = _aes.CreateDecryptor();
            return Encoding.UTF8.GetString(transform.TransformFinalBlock(encryptBytes, 0, encryptBytes.Length));
        }
    }
}
