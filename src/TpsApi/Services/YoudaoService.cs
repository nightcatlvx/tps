using Common.Abstractions;
using Common.DI;
using TpsApi.Client;
using TpsApi.Client.RateLimit;
using TpsApi.Param.Youdao;
using TpsApi.Request;
using TpsApi.Response;

namespace TpsApi.Services;

/// <summary>
/// 有道翻译服务
/// </summary>
public class YoudaoService : BaseService, IBaseAutofac
{
    private readonly IExternalClient _client;

    public YoudaoService(IExternalClient client)
    {
        _client = client;
    }

    public async Task<ResponseMessage<TranslateResponse>> TranslateAsync(TranslateRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Text))
            return ResponseMessage<TranslateResponse>.IsFailed(ApiStatusCode.Biz.ParamError, "原文不能为空");

        try
        {
            var result = await _client.TranslateAsync(new YoudaoTranslateParam
            {
                q = request.Text,
                to = request.TargetLang
            });

            if (result == null || string.IsNullOrEmpty(result.GetFirstTranslation()))
                return ResponseMessage<TranslateResponse>.IsFailed(ApiStatusCode.Biz.Failed, "翻译服务无响应");

            if (!result.IsSuccess)
                return ResponseMessage<TranslateResponse>.IsFailed(ApiStatusCode.Biz.Failed, result.errorCode);

            return ResponseMessage<TranslateResponse>.IsSuccess(new TranslateResponse
            {
                SourceText = request.Text,
                TranslatedText = result.GetFirstTranslation()
            });
        }
        catch (RateLimitException)
        {
            return ResponseMessage<TranslateResponse>.IsFailed(ApiStatusCode.ThirdParty.RateLimited, "请求频繁，请稍后再试");
        }
        catch (Exception ex)
        {
            return ResponseMessage<TranslateResponse>.IsFailed(ApiStatusCode.Biz.Failed, "翻译异常: " + ex.Message);
        }
    }
}
