namespace TpsApi.Client.Auth;

/// <summary>
/// 服务授权抽象接口
/// 每个需要授权的第三方服务实现此接口
/// </summary>
public interface IServiceAuth
{
    /// <summary>
    /// 对应的平台标识
    /// </summary>
    string ServiceCode { get; }

    /// <summary>
    /// 将 Token 注入到请求中
    /// </summary>
    Task InjectAsync(HttpRequestMessage request, CancellationToken ct = default);

    /// <summary>
    /// 清除缓存 Token
    /// </summary>
    void Invalidate();
}
