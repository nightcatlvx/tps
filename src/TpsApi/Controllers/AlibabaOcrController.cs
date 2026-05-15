using Common.Abstractions;
using TpsApi.Client;
using TpsApi.Client.RateLimit;
using Microsoft.AspNetCore.Mvc;

namespace TpsApi.Controllers;

/// <summary>
/// 阿里云 OCR API — 演示 SDK 调用模式
/// </summary>
public class AlibabaOcrController : BaseController
{
    public IExternalClient Client { get; set; } = null!;

    [HttpGet("alibaba/ocr/keywords")]
    public async Task<ResponseMessage<List<string>>> GetKeywords([FromQuery] string picUrl)
    {
        if (string.IsNullOrWhiteSpace(picUrl))
            return ResponseMessage<List<string>>.IsFailed(ApiStatusCode.Biz.ParamError, "picUrl 不能为空");

        try
        {
            var keywords = await Client.GetKeywordsListAsync(picUrl);
            return ResponseMessage<List<string>>.IsSuccess(keywords);
        }
        catch (RateLimitException)
        {
            return ResponseMessage<List<string>>.IsFailed(ApiStatusCode.ThirdParty.RateLimited, "请求频繁");
        }
        catch (Exception ex)
        {
            return ResponseMessage<List<string>>.IsFailed(ApiStatusCode.Biz.Failed, "识别失败: " + ex.Message);
        }
    }
}
