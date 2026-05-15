using Common.Abstractions;
using TpsApi.Request;
using TpsApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace TpsApi.Controllers;

/// <summary>
/// 企业微信 API
/// </summary>
public class QywxController : BaseController
{
    public QywxService Service { get; set; } = null!;

    [HttpPost("qywx/batch-send")]
    public async Task<ResponseMessage<List<Response.QywxSendResultResponse>>> BatchSend(
        [FromBody] QywxBatchSendRequest request)
    {
        var results = await Service.BatchSendByMobileAsync(request.Mobiles, request.Message);
        return ResponseMessage<List<Response.QywxSendResultResponse>>.IsSuccess(results);
    }
}
