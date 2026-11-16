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
