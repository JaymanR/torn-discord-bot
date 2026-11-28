using System.Security.Cryptography;
using System.Text;
using TornDiscordBot.Security;

namespace TornDiscordBot.Tests.Security;

/// <summary>Tests for <see cref="SecretProtector"/>.</summary>
public class SecretProtectorTests
{
    private const string Secret = "AbCdEfGh12345678";
    private const ulong Owner = 42;

    private readonly SecretProtector _protector = new(RandomNumberGenerator.GetBytes(32));

    [Fact]
    public void Unprotect_ReturnsOriginalSecret()
    {
        var blob = _protector.Protect(Secret, Owner);

        Assert.Equal(Secret, _protector.Unprotect(blob, Owner));
    }

    [Fact]
    public void Protect_DoesNotContainPlaintext()
    {
        var blob = _protector.Protect(Secret, Owner);

        Assert.DoesNotContain(Secret, Encoding.Latin1.GetString(blob));
    }

    [Fact]
    public void Protect_ProducesDifferentBlobEachTime()
    {
        Assert.NotEqual(_protector.Protect(Secret, Owner), _protector.Protect(Secret, Owner));
    }

    [Fact]
    public void Unprotect_RejectsAnotherOwner()
    {
        var blob = _protector.Protect(Secret, Owner);

        Assert.ThrowsAny<CryptographicException>(() => _protector.Unprotect(blob, Owner + 1));
    }

    [Fact]
    public void Unprotect_RejectsTamperedBlob()
    {
        var blob = _protector.Protect(Secret, Owner);
        blob[^1] ^= 1;

        Assert.ThrowsAny<CryptographicException>(() => _protector.Unprotect(blob, Owner));
    }

    [Fact]
    public void Unprotect_RejectsAnotherMasterKey()
    {
        var blob = _protector.Protect(Secret, Owner);
        var other = new SecretProtector(RandomNumberGenerator.GetBytes(32));

        Assert.ThrowsAny<CryptographicException>(() => other.Unprotect(blob, Owner));
    }

    [Fact]
    public void Unprotect_RejectsTruncatedBlob()
    {
        Assert.ThrowsAny<CryptographicException>(() => _protector.Unprotect(new byte[10], Owner));
    }

    [Theory]
    [InlineData("not base64!!")]
    [InlineData("AAAAAAAAAAAAAAAAAAAAAA==")] // valid base64, but only 16 bytes
    public void FromBase64_RejectsInvalidKey(string key)
    {
        Assert.Throws<InvalidOperationException>(() => SecretProtector.FromBase64(key));
    }
}
