using System.Security.Cryptography;
using System.Text;

namespace VNCManager.Services;

public static class PasswordService
{
    public static string Protect(string plainText)
    {
        if (string.IsNullOrEmpty(plainText)) return "";
        var data = Encoding.UTF8.GetBytes(plainText);
        var protectedData = ProtectedData.Protect(data, null, DataProtectionScope.CurrentUser);
        return Convert.ToBase64String(protectedData);
    }

    public static string Unprotect(string protectedText)
    {
        if (string.IsNullOrEmpty(protectedText)) return "";
        try
        {
            var data = Convert.FromBase64String(protectedText);
            var plain = ProtectedData.Unprotect(data, null, DataProtectionScope.CurrentUser);
            return Encoding.UTF8.GetString(plain);
        }
        catch
        {
            return "";
        }
    }
}
