using SupportDesk.Infrastructure.Services;

namespace SupportDesk.UnitTests.Infrastructure.Services;

public class Pbkdf2PasswordHasherTests
{
    // Few iterations, so the tests are quick. The real default is much higher.
    private readonly Pbkdf2PasswordHasher _hasher = new(iterations: 1000);

    [Fact]
    public void Hash_DoesNotContainThePassword() =>
        Assert.DoesNotContain("correct horse", _hasher.Hash("correct horse"));

    [Fact]
    public void Verify_AcceptsTheRightPassword() =>
        Assert.True(_hasher.Verify("correct horse", _hasher.Hash("correct horse")));

    [Fact]
    public void Verify_RejectsAWrongPassword() =>
        Assert.False(_hasher.Verify("battery staple", _hasher.Hash("correct horse")));

    [Fact]
    public void Hash_UsesAFreshSaltEveryTime()
    {
        var first = _hasher.Hash("correct horse");
        var second = _hasher.Hash("correct horse");

        Assert.NotEqual(first, second);
        Assert.True(_hasher.Verify("correct horse", first));
        Assert.True(_hasher.Verify("correct horse", second));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not-a-hash")]
    [InlineData("v1.x.y.z")]
    [InlineData("v9.1000.AAAA.AAAA")]
    public void Verify_NeverMatchesAMissingOrUnreadableHash(string? storedHash) =>
        Assert.False(_hasher.Verify("correct horse", storedHash));

    [Fact]
    public void Verify_UsesTheIterationsStoredInTheHash()
    {
        var oldHash = new Pbkdf2PasswordHasher(iterations: 1000).Hash("correct horse");

        Assert.True(new Pbkdf2PasswordHasher(iterations: 2000).Verify("correct horse", oldHash));
    }
}
