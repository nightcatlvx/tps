using Common.Abstractions;
using Common.DI;
using Microsoft.AspNetCore.Mvc;

namespace TpsApi.Controllers;

/// <summary>
/// 控制器基类
/// </summary>
[Route("api/tps/v1")]
[ApiController]
public abstract class BaseController : ControllerBase, IBaseAutofac
{
    /// <summary>
    /// 返回成功
    /// </summary>
    protected IActionResult Success<T>(T data, string message = "操作成功")
        => Ok(ResponseMessage<T>.IsSuccess(data, message));

    /// <summary>
    /// 返回失败
    /// </summary>
    protected IActionResult Failed(int code, string message)
        => Ok(ResponseMessage<object>.IsFailed(code, message));

    /// <summary>
    /// 返回失败（使用 ApiStatusCode）
    /// </summary>
    protected IActionResult BizFailed(string message)
        => Ok(ResponseMessage<object>.IsFailed(ApiStatusCode.Biz.Failed, message));
}
