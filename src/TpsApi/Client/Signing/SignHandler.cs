using TpsApi.Client.Core;
using Microsoft.Extensions.Logging;

namespace TpsApi.Client.Signing;

/// <summary>
/// 签名 DelegatingHandler
/// 挂入 HttpClient 管道，请求发出前自动注入签名
/// </summary>
public class SignHandler : DelegatingHandler
{
    private readonly IEnumerable<IRequestSigner> _signers;
    private readonly ILogger<SignHandler> _logger;
    internal static readonly AsyncLocal<RequestMetadata?> CurrentMeta = new();

    public SignHandler(IEnumerable<IRequestSigner> signers, ILogger<SignHandler> logger)
    {
        _signers = signers;
        _logger = logger;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken ct)
    {
        var meta = CurrentMeta.Value;

        if (meta == null || !meta.IsSignRequired)
            return await base.SendAsync(request, ct);

        var signer = _signers.FirstOrDefault(s => s.ServiceCode == meta.ServiceCode)
            ?? throw new InvalidOperationException(
                $"未找到 ServiceCode={meta.ServiceCode} 的签名实现");

        _logger.LogInformation(
            "[Sign] 开始签名 ServiceCode={ServiceCode} Method={Method}",
            meta.ServiceCode, meta.MethodName);

        await signer.SignAsync(request, ct);

        return await base.SendAsync(request, ct);
    }
}
