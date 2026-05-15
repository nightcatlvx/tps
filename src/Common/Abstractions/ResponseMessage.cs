namespace Common.Abstractions;

/// <summary>
/// 统一响应模型
/// </summary>
public class ResponseMessage<T>
{
    public bool Success { get; set; }
    public int Code { get; set; }
    public string Message { get; set; } = string.Empty;
    public T? Data { get; set; }

    public static ResponseMessage<T> IsSuccess(T data, string message = "操作成功")
        => new() { Success = true, Code = 0, Message = message, Data = data };

    public static ResponseMessage<T> IsFailed(int code, string message)
        => new() { Success = false, Code = code, Message = message };
}
