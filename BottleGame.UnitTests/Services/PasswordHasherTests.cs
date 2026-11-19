using BottleGame.Infrastructure.Services;
using FluentAssertions;
using Microsoft.Extensions.Configuration;

namespace BottleGame.UnitTests.Services;

/// <summary>
/// Tests for PasswordHasher
/// </summary>
public class PasswordHasherTests
{
    /// <summary>
    /// Hasher
    /// </summary>
    private readonly PasswordHasher _hasher;

    /// <summary>
    /// Creates the test fixture: the database, the mocks and the object under test
    /// </summary>
    public PasswordHasherTests()
    {
        var configData = new Dictionary<string, string?>
        {
            { "Security:PasswordHashSecretKey", "test_secret_key_for_hashing_12345" }
        };

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();

        _hasher = new PasswordHasher(config);
    }

    /// <summary>
    /// Hashing a password gives a non-empty string
    /// </summary>
    [Fact]
    public void Hash_ReturnsNonEmptyString()
    {
        // Arrange
        var input = "testPassword123";

        // Act
        var hash = _hasher.Hash(input);

        // Assert
        hash.Should().NotBeNullOrEmpty();
    }

    /// <summary>
    /// The same password and salt always give the same hash
    /// </summary>
    [Fact]
    public void Hash_SameInput_ReturnsSameHash()
    {
        // Arrange
        var input = "testPassword123";

        // Act
        var hash1 = _hasher.Hash(input);
        var hash2 = _hasher.Hash(input);

        // Assert
        hash1.Should().Be(hash2);
    }

    /// <summary>
    /// Different passwords give different hashes
    /// </summary>
    [Fact]
    public void Hash_DifferentInputs_ReturnsDifferentHashes()
    {
        // Arrange
        var input1 = "testPassword123";
        var input2 = "differentPassword456";

        // Act
        var hash1 = _hasher.Hash(input1);
        var hash2 = _hasher.Hash(input2);

        // Assert
        hash1.Should().NotBe(hash2);
    }

    /// <summary>
    /// Hashing with a new salt returns both the hash and the salt
    /// </summary>
    [Fact]
    public void HashWithSalt_ReturnsHashAndSalt()
    {
        // Arrange
        var password = "testPassword123";

        // Act
        var (hash, salt) = _hasher.HashWithSalt(password);

        // Assert
        hash.Should().NotBeNullOrEmpty();
        salt.Should().NotBeNullOrEmpty();
    }

    /// <summary>
    /// The correct password passes the check against its hash and salt
    /// </summary>
    [Fact]
    public void Verify_CorrectPassword_ReturnsTrue()
    {
        // Arrange
        var password = "testPassword123";
        var (hash, salt) = _hasher.HashWithSalt(password);

        // Act
        var result = _hasher.Verify(hash, password, salt);

        // Assert
        result.Should().BeTrue();
    }

    /// <summary>
    /// New hashes use the PBKDF2 format with the number of rounds in front
    /// </summary>
    [Fact]
    public void HashWithSalt_UsesPbkdf2Format()
    {
        var (hash, _) = _hasher.HashWithSalt("testPassword123");

        hash.Should().StartWith("v2$600000$");
        _hasher.NeedsRehash(hash).Should().BeFalse();
    }

    /// <summary>
    /// A hash of the first version, one HMAC round, still verifies and is marked for replacement
    /// </summary>
    [Fact]
    public void Verify_LegacyHash_StillWorksAndNeedsRehash()
    {
        const string salt = "c29tZS1zYWx0LTEyMzQ1Ng==";
        var legacy = _hasher.Hash("testPassword123" + salt);

        _hasher.Verify(legacy, "testPassword123", salt).Should().BeTrue();
        _hasher.Verify(legacy, "wrong", salt).Should().BeFalse();
        _hasher.NeedsRehash(legacy).Should().BeTrue();
    }

    /// <summary>
    /// A stored hash with a damaged or absurd number of rounds is refused instead of being computed
    /// </summary>
    [Theory]
    [InlineData("v2$1$AAAA")]
    [InlineData("v2$999999999$AAAA")]
    [InlineData("v2$not-a-number$AAAA")]
    [InlineData("v2$600000")]
    [InlineData("v2$600000$not base64!")]
    public void Verify_DamagedStoredHash_ReturnsFalse(string stored)
    {
        _hasher.Verify(stored, "testPassword123", "c29tZS1zYWx0LTEyMzQ1Ng==").Should().BeFalse();
    }

    /// <summary>
    /// The same password with another salt gives another hash
    /// </summary>
    [Fact]
    public void HashWithSalt_SamePasswordTwice_GivesDifferentHashes()
    {
        var first = _hasher.HashWithSalt("testPassword123");
        var second = _hasher.HashWithSalt("testPassword123");

        first.Hash.Should().NotBe(second.Hash);
        first.Salt.Should().NotBe(second.Salt);
    }

    /// <summary>
    /// A wrong password fails the check
    /// </summary>
    [Fact]
    public void Verify_IncorrectPassword_ReturnsFalse()
    {
        // Arrange
        var password = "testPassword123";
        var wrongPassword = "wrongPassword";
        var (hash, salt) = _hasher.HashWithSalt(password);

        // Act
        var result = _hasher.Verify(hash, wrongPassword, salt);

        // Assert
        result.Should().BeFalse();
    }
}
