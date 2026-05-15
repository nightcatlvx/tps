using System.Security.Cryptography;
using System.Text;
using Common.Configs;
using TpsApi.Repositories;
using Microsoft.Extensions.Logging;

namespace TpsApi.Client.Signing.Signers;

/// <summary>
/// 有道签名 — SHA256(appKey + input + salt + curtime + appSecret)
/// </summary>
public class YoudaoSigner : IRequestSigner
{
    private readonly ServiceConfigRepository _serviceConfigRepo;
    private readonly ILogger<YoudaoSigner> _logger;

    public string ServiceCode => "youdao";

    public YoudaoSigner(ServiceConfigRepository serviceConfigRepo, ILogger<YoudaoSigner> logger)
    {
        _serviceConfigRepo = serviceConfigRepo;
        _logger = logger;
    }

    public async Task SignAsync(HttpRequestMessage request, CancellationToken ct = default)
    {
        var config = await _serviceConfigRepo.GetByServiceCodeAsync(ServiceCode)
            ?? throw new InvalidOperationException($"未找到服务配置 ServiceCode={ServiceCode}");

        var cfg = config.GetConfig<YoudaoConfig>();
        var formData = await ReadFormDataAsync(request, ct);

        var q = formData.TryGetValue("q", out var qVal) ? qVal :
                formData.TryGetValue("img", out var imgVal) ? imgVal : string.Empty;

        var salt = Guid.NewGuid().ToString();
        var curtime = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds().ToString();
        var sign = CalculateSign(cfg.AppKey, cfg.AppSecret, q, salt, curtime);

        formData["appKey"] = cfg.AppKey;
        formData["salt"] = salt;
        formData["curtime"] = curtime;
        formData["signType"] = "v3";
        formData["sign"] = sign;

        request.Content = new FormUrlEncodedContent(
            formData.Select(kv => new KeyValuePair<string, string>(kv.Key, kv.Value)));

        _logger.LogInformation("[YoudaoSigner] 签名完成 AppKey={AppKey}", cfg.AppKey);
    }

    private static string CalculateSign(string appKey, string appSecret, string q, string salt, string curtime)
    {
        var input = GetInput(q);
        var src = appKey + input + salt + curtime + appSecret;
        return BitConverter.ToString(SHA256.HashData(Encoding.UTF8.GetBytes(src))).Replace("-", "");
    }

    private static string GetInput(string q)
    {
        if (string.IsNullOrEmpty(q)) return string.Empty;
        var len = q.Length;
        return len <= 20 ? q : q[..10] + len + q[^10..];
    }

    private static async Task<Dictionary<string, string>> ReadFormDataAsync(HttpRequestMessage request, CancellationToken ct)
    {
        if (request.Content == null) return new(StringComparer.OrdinalIgnoreCase);

        var raw = await request.Content.ReadAsStringAsync(ct);
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var pair in raw.Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = pair.Split('=', 2);
            if (parts.Length == 2)
                result[Uri.UnescapeDataString(parts[0])] = Uri.UnescapeDataString(parts[1]);
        }
        return result;
    }
}
