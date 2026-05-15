using Common.Abstractions;
using TpsApi.Request;
using TpsApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace TpsApi.Controllers;

/// <summary>
/// 有道翻译 API
/// </summary>
public class YoudaoController : BaseController
{
    public YoudaoService Service { get; set; } = null!;

    [HttpPost("youdao/translate")]
    public async Task<ResponseMessage<Response.TranslateResponse>> Translate([FromBody] TranslateRequest request)
    {
        return await Service.TranslateAsync(request);
    }
}
