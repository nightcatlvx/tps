namespace TpsApi.Client.Signing;

/// <summary>
/// 请求签名抽象接口
/// </summary>
public interface IRequestSigner
{
    string ServiceCode { get; }
    Task SignAsync(HttpRequestMessage request, CancellationToken ct = default);
}
