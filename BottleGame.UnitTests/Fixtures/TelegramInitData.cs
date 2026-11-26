using System.Security.Cryptography;
using System.Text;
using System.Web;

namespace BottleGame.UnitTests.Fixtures;

/// <summary>
/// Builds Telegram mini-app initData signed the way Telegram does it
/// </summary>
public static class TelegramInitData
{
    /// <summary>
    /// Bot token the test data is signed with
    /// </summary>
    public const string BOT_TOKEN = "123456:test-bot-token";

    /// <summary>
    /// Builds signed initData for a user, with an optional start_param
    /// </summary>
    /// <param name="telegramId">Telegram id of the user</param>
    /// <param name="startParam">start_param of the mini-app link, null leaves it out</param>
    /// <param name="botToken">Token to sign with</param>
    /// <returns>Signed initData as a query string</returns>
    public static string Signed(long telegramId, string? startParam = null, string botToken = BOT_TOKEN)
    {
        var fields = new SortedDictionary<string, string>(StringComparer.Ordinal)
        {
            ["auth_date"] = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(),
            ["query_id"] = "AAHdF6IQAAAAAN0XohDhrOrc",
            ["user"] = $"{{\"id\":{telegramId},\"first_name\":\"Demo\"}}",
        };

        if (startParam is not null)
        {
            fields["start_param"] = startParam;
        }

        var checkString = string.Join("\n", fields.Select(f => $"{f.Key}={f.Value}"));
        var secret = HMACSHA256.HashData(Encoding.UTF8.GetBytes("WebAppData"), Encoding.UTF8.GetBytes(botToken));
        var hash = Convert.ToHexStringLower(HMACSHA256.HashData(secret, Encoding.UTF8.GetBytes(checkString)));

        return string.Join("&", fields.Select(f => $"{f.Key}={HttpUtility.UrlEncode(f.Value)}")) + $"&hash={hash}";
    }
}
