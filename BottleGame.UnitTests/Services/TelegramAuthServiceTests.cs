using System.Security.Cryptography;
using System.Text;
using System.Web;
using BottleGame.Infrastructure.Services;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace BottleGame.UnitTests.Services;

/// <summary>
/// Tests for TelegramAuthService
/// </summary>
public class TelegramAuthServiceTests
{
    /// <summary>
    /// Bot token used to sign the test data
    /// </summary>
    private const string BotToken = "123456:test-bot-token";

    /// <summary>
    /// Creates the service with the test bot token and an optional maximum age of initData
    /// </summary>
    /// <param name="maxAgeMinutes">Maximum age in minutes, null keeps the default</param>
    /// <returns>The service under test</returns>
    private static TelegramAuthService CreateService(int? maxAgeMinutes = null)
    {
        var settings = new Dictionary<string, string?> { ["TelegramBotToken"] = BotToken };
        if (maxAgeMinutes is not null)
        {
            settings["Telegram:InitDataMaxAgeMinutes"] = maxAgeMinutes.Value.ToString();
        }

        var config = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        return new TelegramAuthService(config, NullLogger<TelegramAuthService>.Instance);
    }

    /// <summary>
    /// Builds initData signed the way Telegram does it
    /// </summary>
    /// <param name="authDate">Time Telegram signed the data</param>
    /// <param name="botToken">Token to sign with</param>
    /// <returns>Signed initData as a query string</returns>
    private static string SignedInitData(DateTimeOffset authDate, string botToken = BotToken)
    {
        var fields = new SortedDictionary<string, string>(StringComparer.Ordinal)
        {
            ["auth_date"] = authDate.ToUnixTimeSeconds().ToString(),
            ["query_id"] = "AAHdF6IQAAAAAN0XohDhrOrc",
            ["user"] = "{\"id\":5001,\"first_name\":\"Demo\"}",
        };

        var newLine = ((char)10).ToString();
        var checkString = string.Join(newLine, fields.Select(f => $"{f.Key}={f.Value}"));
        var secret = HMACSHA256.HashData(Encoding.UTF8.GetBytes("WebAppData"), Encoding.UTF8.GetBytes(botToken));
        var hash = Convert.ToHexStringLower(HMACSHA256.HashData(secret, Encoding.UTF8.GetBytes(checkString)));

        return string.Join("&", fields.Select(f => $"{f.Key}={HttpUtility.UrlEncode(f.Value)}")) + $"&hash={hash}";
    }

    /// <summary>
    /// Fresh data signed with the right token is accepted
    /// </summary>
    [Fact]
    public void ValidateInitData_FreshAndSigned_ReturnsTrue()
    {
        CreateService().ValidateInitData(SignedInitData(DateTimeOffset.UtcNow)).Should().BeTrue();
    }

    /// <summary>
    /// Data signed with another bot token is rejected
    /// </summary>
    [Fact]
    public void ValidateInitData_WrongToken_ReturnsFalse()
    {
        CreateService().ValidateInitData(SignedInitData(DateTimeOffset.UtcNow, "999:another-token")).Should().BeFalse();
    }

    /// <summary>
    /// A changed field breaks the signature
    /// </summary>
    [Fact]
    public void ValidateInitData_TamperedField_ReturnsFalse()
    {
        var data = SignedInitData(DateTimeOffset.UtcNow).Replace("5001", "5002");

        CreateService().ValidateInitData(data).Should().BeFalse();
    }

    /// <summary>
    /// Data without a hash is rejected
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData("auth_date=1")]
    [InlineData("hash=not-hex&auth_date=1")]
    public void ValidateInitData_MissingOrBrokenHash_ReturnsFalse(string data)
    {
        CreateService().ValidateInitData(data).Should().BeFalse();
    }

    /// <summary>
    /// A valid signature is not enough when the data is older than the allowed age, otherwise a leaked initData works for ever
    /// </summary>
    [Fact]
    public void ValidateInitData_SignedButExpired_ReturnsFalse()
    {
        var data = SignedInitData(DateTimeOffset.UtcNow.AddDays(-2));

        CreateService().ValidateInitData(data).Should().BeFalse();
    }

    /// <summary>
    /// A maximum age of zero turns the freshness check off
    /// </summary>
    [Fact]
    public void ValidateInitData_ExpiredWithCheckOff_ReturnsTrue()
    {
        var data = SignedInitData(DateTimeOffset.UtcNow.AddDays(-2));

        CreateService(maxAgeMinutes: 0).ValidateInitData(data).Should().BeTrue();
    }
}
