using Common.Abstractions;
using Common.Configs;
using Common.DI;
using TpsApi.Client;
using TpsApi.Client.RateLimit;
using TpsApi.Param.Qywx;
using TpsApi.Repositories;
using TpsApi.Request;
using TpsApi.Response;

namespace TpsApi.Services;

/// <summary>
/// 企业微信服务
/// </summary>
public class QywxService : BaseService, IBaseAutofac
{
    private readonly IExternalClient _client;
    private readonly ServiceConfigRepository _serviceConfigRepo;
    private readonly WechatUserRepository _wechatUserRepo;
    private readonly ILogger<QywxService> _logger;

    public QywxService(
        IExternalClient client,
        ServiceConfigRepository serviceConfigRepo,
        WechatUserRepository wechatUserRepo,
        ILogger<QywxService> logger)
    {
        _client = client;
        _serviceConfigRepo = serviceConfigRepo;
        _wechatUserRepo = wechatUserRepo;
        _logger = logger;
    }

    public async Task<List<QywxSendResultResponse>> BatchSendByMobileAsync(
        List<string> mobiles, string message)
    {
        var results = new List<QywxSendResultResponse>();
        if (mobiles == null || mobiles.Count == 0 || string.IsNullOrWhiteSpace(message))
            return results;

        var serviceConfig = await _serviceConfigRepo.GetByServiceCodeAsync("qywx");
        var agentId = serviceConfig?.GetConfig<QywxConfig>().AgentId ?? string.Empty;

        foreach (var mobile in mobiles)
        {
            if (string.IsNullOrWhiteSpace(mobile)) continue;

            var result = new QywxSendResultResponse { Mobile = mobile, Code = 1 };

            try
            {
                var userId = await GetOrFetchUserIdAsync(mobile);
                if (string.IsNullOrEmpty(userId))
                {
                    result.Success = false;
                    result.Message = "获取 userid 失败";
                    results.Add(result);
                    continue;
                }

                var sendResp = await _client.SendMessageAsync(new QywxSendMessageParam
                {
                    touser = userId,
                    agentid = agentId,
                    text = new QywxTextContent { content = message }
                });

                result.Success = sendResp.IsSuccess;
                result.Code = sendResp.errcode;
                result.Message = sendResp.IsSuccess ? "发送成功" : $"发送失败: {sendResp.errmsg}";
            }
            catch (RateLimitException ex)
            {
                result.Success = false;
                result.Message = "请求过于频繁，请稍后再试";
                _logger.LogWarning("[Qywx] 限流 Mobile={Mobile} Reason={Reason}", mobile, ex.Reason);
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Message = "发送异常";
                _logger.LogError(ex, "[Qywx] 异常 Mobile={Mobile}", mobile);
            }

            results.Add(result);
        }

        return results;
    }

    private async Task<string?> GetOrFetchUserIdAsync(string mobile)
    {
        var cached = await _wechatUserRepo.GetByMobileAsync(mobile);
        if (cached != null) return cached.wxid;

        var resp = await _client.GetUserIdAsync(new QywxGetUserIdParam { mobile = mobile });
        if (!resp.IsSuccess) return null;

        try
        {
            await _wechatUserRepo.InsertAsync(mobile, resp.userid);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[Qywx] 写入缓存失败 Mobile={Mobile}", mobile);
        }

        return resp.userid;
    }
}
