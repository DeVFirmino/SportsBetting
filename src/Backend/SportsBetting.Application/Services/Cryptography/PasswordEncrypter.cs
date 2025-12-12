using System.Security.Cryptography;
using System.Text;

namespace SportsBetting.Application.Services.Cryptography;

public class PasswordEncrypter
{
    
    //Generates a byte array from the password and returns it as a string
    public string Encrypt(string password)
    {
        var additional = "ABC";

        var newPassword = $"{password} {additional}";
        
        
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