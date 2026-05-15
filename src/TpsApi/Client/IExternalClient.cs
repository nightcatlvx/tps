using TpsApi.Attributes;
using TpsApi.Client.Sdk.Adapters;
using TpsApi.Param.Youdao;
using TpsApi.Param.Qywx;

namespace TpsApi.Client;

public interface IExternalClient
{
    // ── 有道翻译（HTTP + 签名，无需授权）───────────────────────
    [ApiMethod(Method = "POST", ContentType = "application/x-www-form-urlencoded")]
    [NeedAuth(false)]
    [NeedSign(true)]
    [ServiceCode("youdao")]
    [FuncCode("youdao_translate")]
    [RetryPolicy(RetryCount = 3, WaitSeconds = [1.0, 2.0, 3.0])]
    [CircuitBreaker(BreakAfterFaults = 5, BreakDurationSeconds = 30)]
    [Timeout(TimeoutMs = 10000)]
    Task<YoudaoTranslateResult> TranslateAsync([Body] YoudaoTranslateParam dto);

    // ── 企业微信：获取 token（GET，不授权）───────────────────────
    [ApiMethod(Method = "GET")]
    [NeedAuth(false)]
    [ServiceCode("qywx")]
    [FuncCode("qywx_get_token")]
    [Timeout(TimeoutMs = 5000)]
    Task<QywxTokenResult> GetAccessTokenAsync([Query] string corpid, [Query] string corpsecret);

    // ── 企业微信：手机号换 userid（POST，授权注入 token）─────────
    [ApiMethod(Method = "POST")]
    [NeedAuth(true)]
    [ServiceCode("qywx")]
    [FuncCode("qywx_get_userid")]
    [Timeout(TimeoutMs = 5000)]
    Task<QywxGetUserIdResult> GetUserIdAsync([Body] QywxGetUserIdParam dto);

    // ── 企业微信：发送消息（POST，授权注入 token）─────────────────
    [ApiMethod(Method = "POST")]
    [NeedAuth(true)]
    [ServiceCode("qywx")]
    [FuncCode("qywx_send_message")]
    [Timeout(TimeoutMs = 5000)]
    Task<QywxSendMessageResult> SendMessageAsync([Body] QywxSendMessageParam dto);

    // ── 阿里云 OCR（SDK 调用）───────────────────────────────────
    [ServiceCode("alibaba_ocr")]
    [FuncCode("alibaba_ocr_keywords")]
    [SdkMethod(typeof(AlibabaOcrAdapter))]
    Task<List<string>> GetKeywordsListAsync(string pic_url);
}
