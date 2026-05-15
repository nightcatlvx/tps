namespace Common.Abstractions;

/// <summary>
/// API 状态码定义
/// </summary>
public static class ApiStatusCode
{
    public static class Biz
    {
        public const int Success = 0;
        public const int Failed = -1;
        public const int ParamError = 400;
        public const int Unauthorized = 401;
        public const int Forbidden = 403;
        public const int NotFound = 404;
        public const int ServerError = 500;
    }

    public static class ThirdParty
    {
        public const int ServiceNotFound = 1001;
        public const int FuncNotFound = 1002;
        public const int RateLimited = 1003;
        public const int AuthFailed = 1004;
        public const int SignFailed = 1005;
        public const int RequestFailed = 1006;
    }
}
