using Common.Configs;
using TpsApi.Repositories;

namespace TpsApi.Client.Auth.Providers;

/// <summary>
/// 企业微信授权 — access_token 拼接到 URL QueryString
/// </summary>
public class QywxAuth : IServiceAuth
{
    private readonly ServiceConfigRepository _serviceConfigRepo;
    private readonly TokenManager _tokenManager;
    private readonly Lazy<IExternalClient> _client;
    private readonly ILogger<QywxAuth> _logger;

    public string ServiceCode => "qywx";

    public QywxAuth(
        ServiceConfigRepository serviceConfigRepo,
        TokenManager tokenManager,
        Lazy<IExternalClient> client,
        ILogger<QywxAuth> logger)
    {
        _serviceConfigRepo = serviceConfigRepo;
        _tokenManager = tokenManager;
        _client = client;
        _logger = logger;
    }

    public async Task InjectAsync(HttpRequestMessage request, CancellationToken ct)
    {
        var token = await _tokenManager.GetOrRefreshAsync(ServiceCode, FetchTokenAsync, ct);

        var uri = request.RequestUri!.ToString();
        uri = System.Text.RegularExpressions.Regex.Replace(uri, @"[?&]access_token=[^&]*", "");
        uri += (uri.Contains('?') ? "&" : "?") + $"access_token={token}";
        request.RequestUri = new Uri(uri);
    }

    public void Invalidate() => _tokenManager.Invalidate(ServiceCode);

    private async Task<TokenResult> FetchTokenAsync()
    {
        var cfg = await _serviceConfigRepo.GetByServiceCodeAsync(ServiceCode)
            ?? throw new InvalidOperationException($"未找到服务配置 ServiceCode={ServiceCode}");

        var qywx = cfg.GetConfig<QywxConfig>();
        var resp = await _client.Value.GetAccessTokenAsync(qywx.CorpId, qywx.Secret);

        if (!resp.IsSuccess)
        {
            _logger.LogError("[QywxAuth] 获取 Token 失败 errcode={Code} errmsg={Msg}", resp.errcode, resp.errmsg);
            throw new InvalidOperationException($"企微获取 Token 失败: {resp.errmsg}");
        }

        _logger.LogInformation("[QywxAuth] Token 刷新成功，有效期 {ExpiresIn}s", resp.expires_in);

        return new TokenResult
        {
            Token = resp.access_token,
            ExpiredAt = DateTime.UtcNow.AddSeconds(resp.expires_in - 300)
        };
    }
}
