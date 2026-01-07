using System.Security.Cryptography;
using System.Text;
using SportsBetting.Domain.Security.Cryptography;

namespace SportsBetting.Infrastructure.Security.Cryptography;

public class Sha512Encrypter : IPasswordEncrypter
{
        private readonly string _additionalKey;

        public Sha512Encrypter(string additionalKey)
        {
            _additionalKey = additionalKey;
        }

        //Generates a byte array from the password and returns it as a string
        public string Encrypt(string password)
        {
            var newPassword = $"{password} {_additionalKey}";
        
        
            var bytes = Encoding.UTF8.GetBytes(newPassword);
            var hashBytes = SHA512.HashData(bytes);
        
            return StringBytes(hashBytes);
        }

        private static string StringBytes(byte[] bytes)
        {
            var sb = new StringBuilder();
            foreach (var b in bytes)
            {
                var hex = b.ToString("X2");
                sb.Append(hex);
            }
            return sb.ToString();
        }
}