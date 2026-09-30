using System.Security.Cryptography;
using Microsoft.Extensions.Caching.Memory;

namespace Favo.Books.Modules.Identity.Infrastructure.Authentication;

// One-use browser challenges. Multi-instance deployments require sticky routing or a shared atomic store.
internal sealed class GoogleLoginChallenges : IDisposable
{
    private readonly MemoryCache cache = new(new MemoryCacheOptions { SizeLimit = 10000 });
    private readonly object gate = new();
    public string Create()
    {
        var nonce = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        cache.Set(nonce, true, new MemoryCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5), Size = 1 });
        return nonce;
    }
    public bool Consume(string nonce)
    {
        lock (gate)
        {
            if (!cache.TryGetValue(nonce, out bool _)) return false;
            cache.Remove(nonce);
            return true;
        }
    }
    public void Dispose() => cache.Dispose();
}
