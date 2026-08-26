using System.Security.Cryptography;

namespace SpendLensDatabase.Models.Auth;

public static class RefreshTokenGenerator
{
    public static (string RawToken, Guid Id, byte[] VerifierHash) Generate()
    {
        var id = Guid.CreateVersion7();
        var verifier = Guid.NewGuid();

        Span<byte> raw = stackalloc byte[32];

        id.TryWriteBytes(raw[..16]);
        verifier.TryWriteBytes(raw[16..]);

        var verifiedHash = SHA256.HashData(raw[16..]);

        return (Convert.ToBase64String(raw), id, verifiedHash);
    }

    public static (Guid Id, Guid Verifier) Split(string rawToken)
    {
        var bytes = Convert.FromBase64String(rawToken);
        return (new Guid(bytes.AsSpan(0, 16)), new Guid(bytes.AsSpan(16, 16)));
    }
}