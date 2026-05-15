namespace TpsApi.Client.Auth;

/// <summary>
/// Token 获取结果
/// </summary>
public class TokenResult
{
    public string Token { get; init; } = string.Empty;
    public DateTime ExpiredAt { get; init; }
    public bool IsExpired => DateTime.UtcNow >= ExpiredAt;
}
