using System.Security.Cryptography;
using System.Text;

namespace EssayChecker.Application.Subscriptions;

/// <summary>PurchaseToken-in indexlənə bilən SHA-256 hash-i (hex).</summary>
public static class PurchaseTokenHasher
{
    public static string Hash(string purchaseToken) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(purchaseToken)));
}
